using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 15 dakikada Trendyol iade bildirimlerini ceker.
/// </summary>
public class TrendyolClaimSyncJob : ISyncJob
{
    public string JobId => "trendyol-claim-sync";
    public string CronExpression => "*/15 * * * *"; // Her 15 dk

    private readonly ILogger<TrendyolClaimSyncJob> _logger;

    public TrendyolClaimSyncJob(ILogger<TrendyolClaimSyncJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol iade sync basliyor...", JobId);

        // TODO: TrendyolAdapter.PullClaimsAsync() cagirilacak

        await Task.CompletedTask;
        _logger.LogInformation("[{JobId}] Trendyol iade sync tamamlandi", JobId);
    }
}
