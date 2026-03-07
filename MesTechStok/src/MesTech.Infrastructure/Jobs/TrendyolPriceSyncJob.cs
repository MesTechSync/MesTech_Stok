using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 6 saatte Trendyol ile fiyat esitlemesi yapar.
/// </summary>
public class TrendyolPriceSyncJob : ISyncJob
{
    public string JobId => "trendyol-price-sync";
    public string CronExpression => "0 */6 * * *"; // Her 6 saat

    private readonly ILogger<TrendyolPriceSyncJob> _logger;

    public TrendyolPriceSyncJob(ILogger<TrendyolPriceSyncJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol fiyat sync basliyor...", JobId);

        // TODO: Degisen fiyatlari TrendyolAdapter.PushPriceUpdateAsync() ile push et

        await Task.CompletedTask;
        _logger.LogInformation("[{JobId}] Trendyol fiyat sync tamamlandi", JobId);
    }
}
