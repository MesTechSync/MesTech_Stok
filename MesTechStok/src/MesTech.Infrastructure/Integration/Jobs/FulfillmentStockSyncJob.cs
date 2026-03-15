using Hangfire;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Factory;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Jobs;

/// <summary>
/// Hangfire recurring job (every 30 minutes):
/// 1. Gets all active fulfillment providers from IFulfillmentProviderFactory.
/// 2. For each provider, queries GetInventoryLevelsAsync() for all tracked product SKUs.
/// 3. Updates ProductWarehouseStock table via IStockSplitService.
/// 4. If total stock changed → fires StockChangedEvent for platform sync.
/// </summary>
public sealed class FulfillmentStockSyncJob
{
    private readonly IFulfillmentProviderFactory _fulfillmentFactory;
    private readonly IStockSplitService _stockSplitService;
    private readonly IProductRepository _productRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<FulfillmentStockSyncJob> _logger;

    public FulfillmentStockSyncJob(
        IFulfillmentProviderFactory fulfillmentFactory,
        IStockSplitService stockSplitService,
        IProductRepository productRepository,
        IDomainEventDispatcher eventDispatcher,
        ILogger<FulfillmentStockSyncJob> logger)
    {
        _fulfillmentFactory = fulfillmentFactory ?? throw new ArgumentNullException(nameof(fulfillmentFactory));
        _stockSplitService = stockSplitService ?? throw new ArgumentNullException(nameof(stockSplitService));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════
    // Main execution
    // ═══════════════════════════════════════════

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[FulfillmentStockSync] Starting sync cycle at {Time}", DateTime.UtcNow);

        var providers = _fulfillmentFactory.GetAll();

        if (providers.Count == 0)
        {
            _logger.LogInformation("[FulfillmentStockSync] No fulfillment providers registered — skipping.");
            return;
        }

        // Load all tracked SKUs once (shared across all providers)
        var products = await _productRepository.GetAllAsync().ConfigureAwait(false);
        var skus = products
            .Where(p => !string.IsNullOrWhiteSpace(p.SKU))
            .Select(p => p.SKU)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

        if (skus.Count == 0)
        {
            _logger.LogInformation("[FulfillmentStockSync] No product SKUs found — skipping.");
            return;
        }

        _logger.LogInformation("[FulfillmentStockSync] Processing {ProviderCount} provider(s), {SkuCount} SKUs",
            providers.Count, skus.Count);

        var domainEvents = new List<StockChangedEvent>();

        foreach (var provider in providers)
        {
            ct.ThrowIfCancellationRequested();

            await SyncProviderAsync(provider, skus, products, domainEvents, ct).ConfigureAwait(false);
        }

        // Dispatch all stock-changed events in bulk
        if (domainEvents.Count > 0)
        {
            _logger.LogInformation("[FulfillmentStockSync] Dispatching {Count} StockChangedEvent(s)", domainEvents.Count);
            await _eventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);
        }

        _logger.LogInformation("[FulfillmentStockSync] Sync cycle completed at {Time}", DateTime.UtcNow);
    }

    // ═══════════════════════════════════════════
    // Per-provider sync
    // ═══════════════════════════════════════════

    private async Task SyncProviderAsync(
        IFulfillmentProvider provider,
        IReadOnlyList<string> skus,
        IReadOnlyList<Domain.Entities.Product> products,
        List<StockChangedEvent> domainEvents,
        CancellationToken ct)
    {
        _logger.LogInformation("[FulfillmentStockSync] Syncing provider: {Center}", provider.Center);

        try
        {
            // Check availability before hammering the API
            var isAvailable = await provider.IsAvailableAsync(ct).ConfigureAwait(false);
            if (!isAvailable)
            {
                _logger.LogWarning("[FulfillmentStockSync] Provider {Center} unavailable — skipping", provider.Center);
                return;
            }

            var inventory = await provider.GetInventoryLevelsAsync(skus, ct).ConfigureAwait(false);

            if (inventory.Stocks.Count == 0)
            {
                _logger.LogInformation("[FulfillmentStockSync] Provider {Center} returned 0 stock records",
                    provider.Center);
                return;
            }

            _logger.LogInformation("[FulfillmentStockSync] Provider {Center}: {Count} inventory records received",
                provider.Center, inventory.Stocks.Count);

            // Build SKU → Product lookup for fast resolution
            var skuToProduct = products
                .Where(p => !string.IsNullOrWhiteSpace(p.SKU))
                .ToDictionary(p => p.SKU!, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var stock in inventory.Stocks)
            {
                ct.ThrowIfCancellationRequested();

                if (!skuToProduct.TryGetValue(stock.SKU, out var product))
                {
                    _logger.LogDebug("[FulfillmentStockSync] SKU '{SKU}' not found in local product catalog — skipping",
                        stock.SKU);
                    continue;
                }

                // Read previous total stock for this product across all warehouses
                var previousTotal = await _stockSplitService
                    .GetTotalAvailableAsync(product.Id, ct).ConfigureAwait(false);

                // Update fulfillment center stock
                await _stockSplitService.UpdateFulfillmentStockAsync(
                    product.Id,
                    provider.Center,
                    stock.AvailableQuantity,
                    ct).ConfigureAwait(false);

                // Re-read total after update
                var newTotal = await _stockSplitService
                    .GetTotalAvailableAsync(product.Id, ct).ConfigureAwait(false);

                // Fire StockChangedEvent only if total changed
                if (newTotal != previousTotal)
                {
                    _logger.LogInformation(
                        "[FulfillmentStockSync] Stock changed for SKU='{SKU}' via {Center}: {Prev} → {New}",
                        stock.SKU, provider.Center, previousTotal, newTotal);

                    domainEvents.Add(new StockChangedEvent(
                        ProductId: product.Id,
                        SKU: product.SKU!,
                        PreviousQuantity: previousTotal,
                        NewQuantity: newTotal,
                        MovementType: StockMovementType.PlatformSync,
                        OccurredAt: DateTime.UtcNow));
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Log per-provider failure but continue with remaining providers
            _logger.LogError(ex, "[FulfillmentStockSync] Provider {Center} sync failed", provider.Center);
        }
    }

    // ═══════════════════════════════════════════
    // Hangfire registration
    // ═══════════════════════════════════════════

    /// <summary>
    /// Registers this job as a Hangfire recurring job (every 30 minutes).
    /// Call from HangfireConfig.RegisterRecurringJobs().
    /// </summary>
    public static void Register()
    {
        RecurringJob.AddOrUpdate<FulfillmentStockSyncJob>(
            "fulfillment-stock-sync",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/30 * * * *"); // every 30 minutes
    }
}
