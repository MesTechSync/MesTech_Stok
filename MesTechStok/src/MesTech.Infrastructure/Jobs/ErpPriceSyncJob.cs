using MesTech.Application.Interfaces.Erp;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// ERP fiyat sync job — gunluk 4 kez calisir (06:00, 12:00, 18:00, 23:00).
/// ERP'den urun fiyatlarini ceker ve MesTech'e gunceller.
/// Simdilk log-only placeholder — real structure.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public class ErpPriceSyncJob : ISyncJob
{
    public string JobId => "erp-price-sync";
    public string CronExpression => "0 6,12,18,23 * * *"; // Gunluk 4x

    private readonly IErpAdapterFactory _factory;
    private readonly ILogger<ErpPriceSyncJob> _logger;

    public ErpPriceSyncJob(IErpAdapterFactory factory, ILogger<ErpPriceSyncJob> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] ERP fiyat sync basliyor...", JobId);

        var providers = _factory.SupportedProviders;
        var totalProcessed = 0;
        var totalFailed = 0;

        foreach (var provider in providers)
        {
            ct.ThrowIfCancellationRequested();

#pragma warning disable CA1031 // Intentional: per-adapter isolation — one failure must not stop others
            try
            {
                var adapter = _factory.GetAdapter(provider);

                // Ping to verify connectivity before price sync
                var isAlive = await adapter.PingAsync(ct);
                if (!isAlive)
                {
                    _logger.LogWarning(
                        "[{JobId}] {Provider} ping failed, skipping price sync", JobId, provider);
                    continue;
                }

                // TODO(v2): Implement price fetch from ERP
                // Phase 2: adapter.GetProductPricesAsync() → update MesTech prices
                _logger.LogInformation(
                    "[{JobId}] {Provider} connected — price sync placeholder (Phase 2)",
                    JobId, provider);

                totalProcessed++;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[{JobId}] ERP fiyat sync iptal edildi ({Provider})", JobId, provider);
                throw;
            }
            catch (Exception ex)
            {
                totalFailed++;
                _logger.LogError(ex,
                    "[{JobId}] {Provider} fiyat sync HATA", JobId, provider);
            }
#pragma warning restore CA1031
        }

        _logger.LogInformation(
            "[{JobId}] ERP fiyat sync tamamlandi: {Processed} providers processed, {Failed} failed",
            JobId, totalProcessed, totalFailed);

        await Task.CompletedTask;
    }
}
