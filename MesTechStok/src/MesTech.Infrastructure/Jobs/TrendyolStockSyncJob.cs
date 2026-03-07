using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 30 dakikada stok delta sync Trendyol'a push eder.
/// </summary>
public class TrendyolStockSyncJob : ISyncJob
{
    public string JobId => "trendyol-stock-sync";
    public string CronExpression => "*/30 * * * *"; // Her 30 dk

    private readonly ILogger<TrendyolStockSyncJob> _logger;

    public TrendyolStockSyncJob(ILogger<TrendyolStockSyncJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol stok sync basliyor...", JobId);

        // TODO: Degisen urunleri bul, TrendyolAdapter.PushStockUpdateAsync() ile push et

        await Task.CompletedTask;
        _logger.LogInformation("[{JobId}] Trendyol stok sync tamamlandi", JobId);
    }
}
