using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MesTech.Infrastructure.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public RedisHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var testKey = "__health_check__";
            await _cache.SetStringAsync(testKey, "ok",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
                },
                cancellationToken);

            var result = await _cache.GetStringAsync(testKey, cancellationToken);
            return result == "ok"
                ? HealthCheckResult.Healthy("Redis baglantisi basarili")
                : HealthCheckResult.Degraded("Redis okuma basarisiz");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis baglantisi basarisiz", ex);
        }
    }
}
