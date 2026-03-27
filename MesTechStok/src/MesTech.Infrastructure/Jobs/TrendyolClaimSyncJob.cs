using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 15 dakikada Trendyol iade bildirimlerini ceker.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class TrendyolClaimSyncJob : ISyncJob
{
    public string JobId => "trendyol-claim-sync";
    public string CronExpression => "*/15 * * * *"; // Her 15 dk

    private readonly IAdapterFactory _factory;
    private readonly ILogger<TrendyolClaimSyncJob> _logger;

    public TrendyolClaimSyncJob(IAdapterFactory factory, ILogger<TrendyolClaimSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol iade sync basliyor...", JobId);

        try
        {
            var adapter = _factory.ResolveCapability<IClaimCapableAdapter>("Trendyol");
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] Trendyol IClaimCapableAdapter bulunamadi, atlaniyor", JobId);
                return;
            }

            var since = DateTime.UtcNow.AddMinutes(-20); // 15dk cron + 5dk overlap
            var claims = await adapter.PullClaimsAsync(since, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Trendyol iade sync tamamlandi: {Count} iade cekildi",
                JobId, claims.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Trendyol iade sync HATA", JobId);
            throw;
        }
    }
}
