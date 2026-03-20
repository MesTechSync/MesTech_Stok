using MesTech.Application.Interfaces.Erp;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// ERP stok sync job — her 15 dakikada calisir.
/// IErpStockCapable implement eden tum ERP adapter'lardan stok seviyelerini ceker.
/// </summary>
public class ErpStockSyncJob : ISyncJob
{
    public string JobId => "erp-stock-sync";
    public string CronExpression => "*/15 * * * *"; // Her 15 dk

    private readonly IErpAdapterFactory _factory;
    private readonly ILogger<ErpStockSyncJob> _logger;

    public ErpStockSyncJob(IErpAdapterFactory factory, ILogger<ErpStockSyncJob> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] ERP stok sync basliyor...", JobId);

        var providers = _factory.SupportedProviders;
        var totalSynced = 0;
        var totalFailed = 0;

        foreach (var provider in providers)
        {
            ct.ThrowIfCancellationRequested();

#pragma warning disable CA1031 // Intentional: per-adapter isolation — one failure must not stop others
            try
            {
                var adapter = _factory.GetAdapter(provider);

                // Only process adapters that support stock capability
                if (adapter is not IErpStockCapable stockCapable)
                {
                    _logger.LogDebug(
                        "[{JobId}] {Provider} does not implement IErpStockCapable, skipping",
                        JobId, provider);
                    continue;
                }

                _logger.LogInformation(
                    "[{JobId}] Fetching stock levels from {Provider}...", JobId, provider);

                var stockItems = await stockCapable.GetStockLevelsAsync(ct);

                if (stockItems.Count == 0)
                {
                    _logger.LogInformation(
                        "[{JobId}] {Provider} returned 0 stock items", JobId, provider);
                    continue;
                }

                // TODO(v2): Update MesTech stock from ERP stock items
                // For each stockItem, find matching MesTech product and update quantity
                _logger.LogInformation(
                    "[{JobId}] {Provider} returned {Count} stock items — ready for MesTech sync",
                    JobId, provider, stockItems.Count);

                totalSynced += stockItems.Count;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobId}] ERP stok sync iptal edildi ({Provider})", JobId, provider);
                throw;
            }
            catch (Exception ex)
            {
                totalFailed++;
                _logger.LogError(ex,
                    "[{JobId}] {Provider} stok sync HATA", JobId, provider);
            }
#pragma warning restore CA1031
        }

        _logger.LogInformation(
            "[{JobId}] ERP stok sync tamamlandi: {Synced} items from {Providers} providers, {Failed} failed",
            JobId, totalSynced, providers.Count, totalFailed);
    }
}
