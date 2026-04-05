using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Orchestration;

/// <summary>
/// Coklu platform orkestrasyon servisi.
/// Paralel sync, per-adapter timeout, event-driven push.
///
/// ARCHITECTURE NOTE (G058): Platform adapter'lar dedicated REST endpoint KULLANMAZ.
/// Akış modeli:
///   1. Hangfire Jobs (15dk/1dk cron) → adapter.PullOrdersAsync / PushStockAsync
///   2. MassTransit Consumers → StockChangedEvent → adapter.PushStockUpdateAsync
///   3. PlatformSyncEndpoint (generic) → POST /api/platform/sync/{platformCode} → this orchestrator
///   4. AdapterHealthService → /api/health/adapters → tüm adapter'lara PingAsync
///
/// Neden dedicated endpoint yok:
///   - Platform sync'leri scheduler-driven (Hangfire), user-initiated değil
///   - Stok/fiyat değişiklikleri event-driven (MassTransit), REST push değil
///   - Manuel tetikleme PlatformSyncEndpoint generic dispatcher üzerinden yapılır
///   - Her adapter IIntegratorAdapter implement eder, orchestrator polimorfik dispatch yapar
/// </summary>
public sealed class IntegratorOrchestratorService : IIntegratorOrchestrator
{
    private readonly IAdapterFactory _factory;
    private readonly ILogger<IntegratorOrchestratorService> _logger;
    private readonly List<IIntegratorAdapter> _registeredAdapters = new();
    private readonly System.Threading.Lock _lock = new();
    private static readonly TimeSpan PerAdapterTimeout = TimeSpan.FromSeconds(30);

    public IntegratorOrchestratorService(
        IAdapterFactory factory,
        ILogger<IntegratorOrchestratorService> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        lock (_lock)
        {
            _registeredAdapters.AddRange(_factory.GetAll());
        }
    }

    public IReadOnlyList<IIntegratorAdapter> RegisteredAdapters
    {
        get { lock (_lock) { return _registeredAdapters.ToList().AsReadOnly(); } }
    }

    public Task RegisterAdapterAsync(IIntegratorAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        lock (_lock)
        {
            if (_registeredAdapters.All(a => !a.PlatformCode.Equals(adapter.PlatformCode, StringComparison.OrdinalIgnoreCase)))
            {
                _registeredAdapters.Add(adapter);
                _logger.LogInformation("Adapter registered: {Platform}", adapter.PlatformCode);
            }
        }
        return Task.CompletedTask;
    }

    public Task RemoveAdapterAsync(string platformCode)
    {
        lock (_lock)
        {
            var removed = _registeredAdapters.RemoveAll(
                a => a.PlatformCode.Equals(platformCode, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
                _logger.LogInformation("Adapter removed: {Platform}", platformCode);
        }
        return Task.CompletedTask;
    }

    public async Task<SyncResultDto> SyncPlatformAsync(string platformCode, CancellationToken ct = default)
    {
        var result = new SyncResultDto
        {
            PlatformCode = platformCode,
            StartedAt = DateTime.UtcNow
        };

        var adapter = _factory.Resolve(platformCode);
        if (adapter is null)
        {
            result.ErrorMessage = $"Platform '{platformCode}' icin adapter bulunamadi.";
            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(PerAdapterTimeout);

            var products = await adapter.PullProductsAsync(cts.Token).ConfigureAwait(false);
            result.ItemsProcessed = products.Count;
            result.IsSuccess = true;

            _logger.LogInformation("SyncPlatform {Platform}: {Count} items synced",
                platformCode, products.Count);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            result.ErrorMessage = $"Platform '{platformCode}' {PerAdapterTimeout.TotalSeconds}s timeout asimi.";
            _logger.LogWarning("SyncPlatform {Platform}: timeout after {Seconds}s",
                platformCode, PerAdapterTimeout.TotalSeconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "SyncPlatform {Platform} failed", platformCode);
        }

        result.CompletedAt = DateTime.UtcNow;
        return result;
    }

    public async Task<SyncResultDto> SyncAllPlatformsAsync(CancellationToken ct = default)
    {
        var batchStart = DateTime.UtcNow;
        IReadOnlyList<IIntegratorAdapter> adapters;
        lock (_lock) { adapters = _registeredAdapters.ToList().AsReadOnly(); }

        _logger.LogInformation("SyncAllPlatforms starting: {Count} adapters", adapters.Count);

        var tasks = adapters.Select(adapter =>
            SyncPlatformAsync(adapter.PlatformCode, ct));

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var batch = new SyncBatchResultDto
        {
            TotalPlatforms = results.Length,
            SuccessCount = results.Count(r => r.IsSuccess),
            FailureCount = results.Count(r => !r.IsSuccess),
            StartedAt = batchStart,
            CompletedAt = DateTime.UtcNow,
            PlatformResults = results.ToList()
        };

        _logger.LogInformation(
            "SyncAllPlatforms complete: {Success}/{Total} succeeded in {Duration}ms",
            batch.SuccessCount, batch.TotalPlatforms, batch.Duration.TotalMilliseconds);

        return new SyncResultDto
        {
            PlatformCode = "ALL",
            IsSuccess = batch.AllSucceeded,
            ItemsProcessed = results.Sum(r => r.ItemsProcessed),
            ItemsFailed = results.Sum(r => r.ItemsFailed),
            StartedAt = batchStart,
            CompletedAt = DateTime.UtcNow,
            Warnings = results
                .Where(r => !r.IsSuccess)
                .Select(r => $"{r.PlatformCode}: {r.ErrorMessage}")
                .ToList()
        };
    }

    public async Task HandleStockChangedAsync(StockChangedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation("HandleStockChanged: SKU={SKU} Qty={Qty}",
            domainEvent.SKU, domainEvent.NewQuantity);

        IReadOnlyList<IIntegratorAdapter> adapters;
        lock (_lock) { adapters = _registeredAdapters.ToList().AsReadOnly(); }

        var stockAdapters = adapters.Where(a => a.SupportsStockUpdate);

        var tasks = stockAdapters.Select(async adapter =>
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(PerAdapterTimeout);

                await adapter.PushStockUpdateAsync(
                    domainEvent.ProductId, domainEvent.NewQuantity, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "StockChanged push failed: {Platform}", adapter.PlatformCode);
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task HandlePriceChangedAsync(PriceChangedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation("HandlePriceChanged: SKU={SKU} Price={Price}",
            domainEvent.SKU, domainEvent.NewPrice);

        IReadOnlyList<IIntegratorAdapter> adapters;
        lock (_lock) { adapters = _registeredAdapters.ToList().AsReadOnly(); }

        var priceAdapters = adapters.Where(a => a.SupportsPriceUpdate);

        var tasks = priceAdapters.Select(async adapter =>
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(PerAdapterTimeout);

                await adapter.PushPriceUpdateAsync(
                    domainEvent.ProductId, domainEvent.NewPrice, cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "PriceChanged push failed: {Platform}", adapter.PlatformCode);
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task HandleProductCreatedAsync(ProductCreatedEvent domainEvent, CancellationToken ct = default)
    {
        _logger.LogInformation("HandleProductCreated: SKU={SKU} Name={Name}",
            domainEvent.SKU, domainEvent.Name);

        // Product push requires full Product entity — log for now,
        // actual push will be handled by a MediatR handler that fetches
        // the full product from the repository.
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
