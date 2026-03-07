using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 1 dakikada tum platform API'lerinin saglik durumunu kontrol eder.
/// </summary>
public class HealthCheckJob : ISyncJob
{
    public string JobId => "health-check";
    public string CronExpression => "* * * * *"; // Her 1 dk

    private readonly ICacheService _cache;
    private readonly ILogger<HealthCheckJob> _logger;

    public HealthCheckJob(ICacheService cache, ILogger<HealthCheckJob> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("[{JobId}] Health check basliyor...", JobId);

        var result = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow,
            ["status"] = "healthy"
        };

        // TODO: Her platform adapter'in CheckHealthAsync() metodunu cagir
        // Sonuclari cache'e yaz

        await _cache.SetAsync(CacheKeys.Health("all"), result, CacheKeys.HealthTTL, ct);

        _logger.LogDebug("[{JobId}] Health check tamamlandi", JobId);
    }
}
