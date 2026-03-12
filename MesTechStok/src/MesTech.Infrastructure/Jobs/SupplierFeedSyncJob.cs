using Hangfire;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Data-driven Hangfire job: her aktif SupplierFeed için ayrı recurring job kaydedilir.
/// Feed URL'den ürün verisi çeker, parse eder, stok/fiyat günceller, hedef platformlara push eder.
/// </summary>
public class SupplierFeedSyncJob
{
    private readonly AppDbContext _dbContext;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdapterFactory _adapterFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SupplierFeedSyncJob> _logger;

    public SupplierFeedSyncJob(
        AppDbContext dbContext,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IAdapterFactory adapterFactory,
        IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider,
        ILogger<SupplierFeedSyncJob> logger)
    {
        _dbContext = dbContext;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _adapterFactory = adapterFactory;
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(Guid feedId, CancellationToken ct = default)
    {
        _logger.LogInformation("[SupplierFeedSync] Starting sync for feed {FeedId}", feedId);

        var feed = await _dbContext.SupplierFeeds
            .FirstOrDefaultAsync(f => f.Id == feedId && !f.IsDeleted, ct);

        if (feed == null)
        {
            _logger.LogWarning("[SupplierFeedSync] Feed {FeedId} not found or deleted, skipping", feedId);
            return;
        }

        if (!feed.IsActive)
        {
            _logger.LogWarning("[SupplierFeedSync] Feed {FeedId} ({FeedName}) is inactive, skipping",
                feedId, feed.Name);
            return;
        }

        feed.LastSyncStatus = FeedSyncStatus.InProgress;
        await _dbContext.SaveChangesAsync(ct);

        int totalProducts = 0;
        int updatedProducts = 0;
        int deactivatedProducts = 0;
        var updatedProductEntities = new List<Product>();

        try
        {
            // 1. Fetch feed data from URL
            _logger.LogInformation("[SupplierFeedSync] Fetching feed from {FeedUrl} for {FeedName}",
                feed.FeedUrl, feed.Name);

            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.GetAsync(feed.FeedUrl, ct);
            response.EnsureSuccessStatusCode();

            await using var feedStream = await response.Content.ReadAsStreamAsync(ct);

            // 2. Resolve keyed IFeedParserService based on feed format
            var parser = _serviceProvider.GetKeyedService<IFeedParserService>(feed.Format);
            if (parser == null)
            {
                var error = $"No IFeedParserService registered for format {feed.Format}";
                _logger.LogError("[SupplierFeedSync] {Error}", error);
                feed.RecordSyncResult(0, 0, 0, error);
                await _unitOfWork.SaveChangesAsync(ct);
                return;
            }

            // 3. Parse the feed with default field mapping
            var mapping = new FeedFieldMapping(null, null, null, null, null, null, null, null);
            var parseResult = await parser.ParseAsync(feedStream, mapping, ct);

            _logger.LogInformation(
                "[SupplierFeedSync] Parsed {TotalParsed} products ({SkippedCount} skipped, {ErrorCount} errors) for {FeedName}",
                parseResult.TotalParsed, parseResult.SkippedCount, parseResult.Errors.Count, feed.Name);

            totalProducts = parseResult.Products.Count;

            // 4. Process each parsed product
            foreach (var parsedProduct in parseResult.Products)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var (product, wasUpdated, wasDeactivated) = await ProcessParsedProductAsync(
                        feed, parsedProduct, ct);

                    if (wasUpdated)
                    {
                        updatedProducts++;
                        updatedProductEntities.Add(product);
                    }

                    if (wasDeactivated)
                        deactivatedProducts++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[SupplierFeedSync] Failed to process product SKU={SKU} Barcode={Barcode} in feed {FeedName}",
                        parsedProduct.SKU, parsedProduct.Barcode, feed.Name);
                }
            }

            // 5. Record sync result (raises SupplierFeedSyncedEvent)
            feed.RecordSyncResult(totalProducts, updatedProducts, deactivatedProducts);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[SupplierFeedSync] Sync completed for {FeedName}: {Total} total, {Updated} updated, {Deactivated} deactivated",
                feed.Name, totalProducts, updatedProducts, deactivatedProducts);

            // 6. Push updates to target platforms
            await PushToTargetPlatformsAsync(feed, updatedProductEntities, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[SupplierFeedSync] Sync cancelled for feed {FeedId} ({FeedName})",
                feedId, feed.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SupplierFeedSync] Sync FAILED for feed {FeedId} ({FeedName})",
                feedId, feed.Name);

            feed.RecordSyncResult(totalProducts, updatedProducts, deactivatedProducts, ex.Message);
            await _unitOfWork.SaveChangesAsync(ct);

            throw;
        }
    }

    private async Task<(Product product, bool wasUpdated, bool wasDeactivated)> ProcessParsedProductAsync(
        SupplierFeed feed, ParsedProduct parsed, CancellationToken ct)
    {
        // Try to find existing product by SKU or Barcode
        Product? existing = null;

        if (!string.IsNullOrWhiteSpace(parsed.SKU))
            existing = await _productRepository.GetBySKUAsync(parsed.SKU);

        if (existing == null && !string.IsNullOrWhiteSpace(parsed.Barcode))
            existing = await _productRepository.GetByBarcodeAsync(parsed.Barcode);

        bool wasUpdated = false;
        bool wasDeactivated = false;

        if (existing != null)
        {
            // Update existing product
            var markedPrice = feed.ApplyMarkup(parsed.Price ?? 0m);
            bool stockChanged = parsed.Quantity.HasValue && existing.Stock != parsed.Quantity.Value;
            bool priceChanged = parsed.Price.HasValue && existing.SalePrice != markedPrice;

            if (stockChanged)
            {
                existing.Stock = parsed.Quantity!.Value;
                existing.LastStockUpdate = DateTime.UtcNow;
                wasUpdated = true;
            }

            if (priceChanged)
            {
                existing.SalePrice = markedPrice;
                wasUpdated = true;
            }

            // Zero stock → deactivate (if configured)
            if (feed.AutoDeactivateOnZeroStock && parsed.Quantity is 0 && existing.IsActive)
            {
                existing.IsActive = false;
                wasDeactivated = true;
                wasUpdated = true;
                _logger.LogInformation(
                    "[SupplierFeedSync] Deactivated product {SKU} (zero stock) in feed {FeedName}",
                    existing.SKU, feed.Name);
            }

            // Restock → reactivate (if configured)
            if (feed.AutoActivateOnRestock && parsed.Quantity > 0 && !existing.IsActive)
            {
                existing.IsActive = true;
                wasUpdated = true;
                _logger.LogInformation(
                    "[SupplierFeedSync] Reactivated product {SKU} (restocked) in feed {FeedName}",
                    existing.SKU, feed.Name);
            }

            if (wasUpdated)
            {
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = "supplier-feed-sync";
                await _productRepository.UpdateAsync(existing);
            }

            return (existing, wasUpdated, wasDeactivated);
        }
        else
        {
            // New product — create
            var newProduct = new Product
            {
                SKU = parsed.SKU ?? $"FEED-{Guid.NewGuid():N}",
                Barcode = parsed.Barcode,
                Name = parsed.Name ?? "Unknown Product",
                Description = parsed.Description,
                SalePrice = feed.ApplyMarkup(parsed.Price ?? 0m),
                PurchasePrice = parsed.Price ?? 0m,
                Stock = parsed.Quantity ?? 0,
                ImageUrl = parsed.ImageUrl,
                Brand = parsed.Brand,
                Model = parsed.Model,
                IsActive = !(feed.AutoDeactivateOnZeroStock && parsed.Quantity is 0),
                SupplierId = feed.SupplierId,
                TenantId = feed.TenantId,
                CreatedBy = "supplier-feed-sync",
                UpdatedBy = "supplier-feed-sync",
                LastStockUpdate = DateTime.UtcNow
            };

            await _productRepository.AddAsync(newProduct);

            _logger.LogDebug(
                "[SupplierFeedSync] Created new product {SKU} from feed {FeedName}",
                newProduct.SKU, feed.Name);

            return (newProduct, true, false);
        }
    }

    private async Task PushToTargetPlatformsAsync(
        SupplierFeed feed, List<Product> updatedProducts, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(feed.TargetPlatforms) || updatedProducts.Count == 0)
            return;

        var platformStrings = feed.TargetPlatforms
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var platformStr in platformStrings)
        {
            if (!Enum.TryParse<PlatformType>(platformStr, ignoreCase: true, out var platformType))
            {
                _logger.LogWarning(
                    "[SupplierFeedSync] Unknown platform '{Platform}' in TargetPlatforms for feed {FeedName}",
                    platformStr, feed.Name);
                continue;
            }

            var adapter = _adapterFactory.Resolve(platformType);
            if (adapter == null)
            {
                _logger.LogWarning(
                    "[SupplierFeedSync] No adapter found for platform {Platform} in feed {FeedName}",
                    platformType, feed.Name);
                continue;
            }

            _logger.LogInformation(
                "[SupplierFeedSync] Pushing {Count} products to {Platform} for feed {FeedName}",
                updatedProducts.Count, platformType, feed.Name);

            int pushed = 0;
            foreach (var product in updatedProducts)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var ok = await adapter.PushProductAsync(product, ct);
                    if (ok) pushed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[SupplierFeedSync] Failed to push product {SKU} to {Platform}",
                        product.SKU, platformType);
                }
            }

            _logger.LogInformation(
                "[SupplierFeedSync] Pushed {Pushed}/{Total} products to {Platform} for feed {FeedName}",
                pushed, updatedProducts.Count, platformType, feed.Name);
        }
    }

    /// <summary>
    /// Registers one Hangfire recurring job per active SupplierFeed.
    /// Called at application startup.
    /// </summary>
    public static void RegisterSupplierFeedJobs(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SupplierFeedSyncJob>>();

        var activeFeeds = dbContext.SupplierFeeds
            .Where(f => f.IsActive && !f.IsDeleted)
            .ToList();

        logger.LogInformation(
            "[SupplierFeedSync] Registering {Count} recurring supplier feed jobs", activeFeeds.Count);

        foreach (var feed in activeFeeds)
        {
            var cronExpression = feed.CronExpression
                ?? $"*/{Math.Max(feed.SyncIntervalMinutes, 1)} * * * *";

            RecurringJob.AddOrUpdate<SupplierFeedSyncJob>(
                $"supplier-feed-{feed.Id}",
                job => job.ExecuteAsync(feed.Id, CancellationToken.None),
                cronExpression);

            logger.LogInformation(
                "[SupplierFeedSync] Registered job 'supplier-feed-{FeedId}' ({FeedName}) with cron '{Cron}'",
                feed.Id, feed.Name, cronExpression);
        }
    }
}
