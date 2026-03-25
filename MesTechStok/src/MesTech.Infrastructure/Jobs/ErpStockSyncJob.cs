using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Constants;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;
using System.Diagnostics;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// ERP stok sync job — her 15 dakikada calisir.
/// IErpStockCapable implement eden tum ERP adapter'lardan stok seviyelerini ceker
/// ve MesTech urunlerinin stok miktarlarini gunceller.
/// Phase-2 TAM: SKU eslestirip Product.AdjustStock cagirir (delta bazli).
/// ErpSyncLog ile her provider icin sync sonucu kaydedilir.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class ErpStockSyncJob : ISyncJob
{
    public string JobId => "erp-stock-sync";
    public string CronExpression => "*/15 * * * *"; // Her 15 dk

    private readonly IErpAdapterFactory _factory;
    private readonly IProductRepository _productRepository;
    private readonly IErpSyncLogRepository _syncLogRepository;
    private readonly ILogger<ErpStockSyncJob> _logger;

    public ErpStockSyncJob(
        IErpAdapterFactory factory,
        IProductRepository productRepository,
        IErpSyncLogRepository syncLogRepository,
        ILogger<ErpStockSyncJob> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _syncLogRepository = syncLogRepository ?? throw new ArgumentNullException(nameof(syncLogRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] ERP stok sync basliyor...", JobId);

        var providers = _factory.SupportedProviders;
        var totalUpdated = 0;
        var totalSkipped = 0;
        var totalFailed = 0;

        foreach (var provider in providers)
        {
            ct.ThrowIfCancellationRequested();

            var sw = Stopwatch.StartNew();
            var syncLog = ErpSyncLog.Create(DomainConstants.SystemTenantId, provider, "StockSync", Guid.NewGuid());
            var providerUpdated = 0;
            var providerSkipped = 0;

#pragma warning disable CA1031 // Intentional: per-adapter isolation — one failure must not stop others
            try
            {
                var adapter = _factory.GetAdapter(provider);

                if (adapter is not IErpStockCapable stockCapable)
                {
                    _logger.LogDebug(
                        "[{JobId}] {Provider} does not implement IErpStockCapable, skipping",
                        JobId, provider);
                    continue;
                }

                _logger.LogInformation(
                    "[{JobId}] Fetching stock levels from {Provider}...", JobId, provider);

                var stockItems = await stockCapable.GetStockLevelsAsync(ct).ConfigureAwait(false);

                if (stockItems.Count == 0)
                {
                    _logger.LogInformation(
                        "[{JobId}] {Provider} returned 0 stock items", JobId, provider);
                    sw.Stop();
                    syncLog.MarkSuccess($"{provider}-stock-0");
                    syncLog.SetDetails(0, 0, 0, 0, sw.ElapsedMilliseconds, triggeredBy: "Hangfire");
                    await _syncLogRepository.AddAsync(syncLog, ct).ConfigureAwait(false);
                    continue;
                }

                foreach (var stockItem in stockItems)
                {
                    ct.ThrowIfCancellationRequested();

                    var product = await _productRepository.GetBySKUAsync(stockItem.ProductCode).ConfigureAwait(false);
                    if (product is null)
                    {
                        providerSkipped++;
                        continue;
                    }

                    var delta = stockItem.Quantity - product.Stock;
                    if (delta == 0)
                    {
                        providerSkipped++;
                        continue;
                    }

                    var movementType = delta > 0 ? StockMovementType.StockIn : StockMovementType.StockOut;
                    product.AdjustStock(delta, movementType, $"ERP sync ({provider})");

                    await _productRepository.UpdateAsync(product).ConfigureAwait(false);
                    providerUpdated++;
                }

                totalUpdated += providerUpdated;
                totalSkipped += providerSkipped;

                sw.Stop();
                syncLog.MarkSuccess($"{provider}-stock-{stockItems.Count}");
                syncLog.SetDetails(stockItems.Count, providerUpdated, 0, providerSkipped, sw.ElapsedMilliseconds, triggeredBy: "Hangfire");
                await _syncLogRepository.AddAsync(syncLog, ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "[{JobId}] {Provider} stock sync: {Count} items fetched, {Updated} updated, {Skipped} skipped",
                    JobId, provider, stockItems.Count, providerUpdated, providerSkipped);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobId}] ERP stok sync iptal edildi ({Provider})", JobId, provider);
                throw;
            }
            catch (Exception ex)
            {
                totalFailed++;
                sw.Stop();
                syncLog.MarkFailure(ex.Message);
                syncLog.SetDetails(0, 0, 1, 0, sw.ElapsedMilliseconds, ex.ToString(), "Hangfire");
                await _syncLogRepository.AddAsync(syncLog, ct).ConfigureAwait(false);

                _logger.LogError(ex,
                    "[{JobId}] {Provider} stok sync HATA", JobId, provider);
            }
#pragma warning restore CA1031
        }

        _logger.LogInformation(
            "[{JobId}] ERP stok sync tamamlandi: {Updated} updated, {Skipped} skipped, {Failed} failed",
            JobId, totalUpdated, totalSkipped, totalFailed);
    }
}
