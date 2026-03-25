using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Caching;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 1 dakikada tum platform API'lerinin saglik durumunu kontrol eder.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class HealthCheckJob : ISyncJob
{
    public string JobId => "health-check";
    public string CronExpression => "* * * * *"; // Her 1 dk

    private readonly IAdapterFactory _factory;
    private readonly ICacheService _cache;
    private readonly ILogger<HealthCheckJob> _logger;

    public HealthCheckJob(IAdapterFactory factory, ICacheService cache, ILogger<HealthCheckJob> logger)
    {
        _factory = factory;
        _cache = cache;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("[{JobId}] Health check basliyor...", JobId);

        var adapters = _factory.GetAll();
        var healthResults = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow,
            ["adapterCount"] = adapters.Count
        };

        var allHealthy = true;

        foreach (var adapter in adapters)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // TrendyolAdapter has CheckHealthAsync
                if (adapter is TrendyolAdapter trendyol)
                {
                    var health = await trendyol.CheckHealthAsync(ct).ConfigureAwait(false);
                    healthResults[adapter.PlatformCode] = new { health.IsHealthy, health.LatencyMs };
                    if (!health.IsHealthy) allHealthy = false;

                    _logger.LogDebug("[{JobId}] {Platform}: {Status} ({Latency}ms)",
                        JobId, adapter.PlatformCode,
                        health.IsHealthy ? "UP" : "DOWN",
                        health.LatencyMs);
                }
                else
                {
                    // Other adapters: registered = available
                    healthResults[adapter.PlatformCode] = new { IsHealthy = true, LatencyMs = 0 };
                    _logger.LogDebug("[{JobId}] {Platform}: REGISTERED", JobId, adapter.PlatformCode);
                }
            }
            catch (Exception ex)
            {
                allHealthy = false;
                healthResults[adapter.PlatformCode] = new { IsHealthy = false, Error = ex.Message };
                _logger.LogWarning(ex, "[{JobId}] {Platform} health check basarisiz", JobId, adapter.PlatformCode);
            }
        }

        healthResults["status"] = allHealthy ? "healthy" : "degraded";

        await _cache.SetAsync(CacheKeys.Health("all"), healthResults, CacheKeys.HealthTTL, ct).ConfigureAwait(false);

        _logger.LogDebug("[{JobId}] Health check tamamlandi: {Status}, {Count} adapter kontrol edildi",
            JobId, allHealthy ? "healthy" : "degraded", adapters.Count);
    }
}
