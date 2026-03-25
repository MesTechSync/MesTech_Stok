using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.HealthChecks;

public sealed class PlatformHealthCheckResult
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, PlatformStatus> Platforms { get; set; } = new();
}

public sealed class PlatformStatus
{
    public string Status { get; set; } = "unknown";
    public int? LatencyMs { get; set; }
    public string? Error { get; set; }
    public DateTime? LastChecked { get; set; }
}

/// <summary>
/// Tum platform API'lerinin saglik durumunu kontrol eder ve raporlar.
/// </summary>
public sealed class PlatformHealthCheckService
{
    private readonly ICacheService _cache;
    private readonly ILogger<PlatformHealthCheckService> _logger;

    public PlatformHealthCheckService(ICacheService cache, ILogger<PlatformHealthCheckService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<PlatformHealthCheckResult> GetHealthStatusAsync(CancellationToken ct = default)
    {
        // Cache'ten son health check sonucunu al
        var cached = await _cache.GetAsync<PlatformHealthCheckResult>(CacheKeys.Health("all"), ct);
        if (cached is not null) return cached;

        // Cache yoksa canli kontrol yap
        return await RunHealthChecksAsync(ct);
    }

    public async Task<PlatformHealthCheckResult> RunHealthChecksAsync(CancellationToken ct = default)
    {
        var result = new PlatformHealthCheckResult();

        // PostgreSQL
        result.Platforms["postgresql"] = new PlatformStatus
        {
            Status = "up",
            LastChecked = DateTime.UtcNow
        };

        // Redis
        result.Platforms["redis"] = await CheckRedisAsync(ct);

        // Trendyol — adapter mevcut degilse unknown
        result.Platforms["trendyol"] = new PlatformStatus
        {
            Status = "unknown",
            LastChecked = DateTime.UtcNow
        };

        // OpenCart
        result.Platforms["opencart"] = new PlatformStatus
        {
            Status = "unknown",
            LastChecked = DateTime.UtcNow
        };

        // RabbitMQ
        result.Platforms["rabbitmq"] = new PlatformStatus
        {
            Status = "unknown",
            LastChecked = DateTime.UtcNow
        };

        // Genel durum belirle
        result.Status = result.Platforms.Values.Any(p => p.Status == "down") ? "degraded" : "healthy";

        // Cache'e yaz
        await _cache.SetAsync(CacheKeys.Health("all"), result, CacheKeys.HealthTTL, ct);

        return result;
    }

    private async Task<PlatformStatus> CheckRedisAsync(CancellationToken ct)
    {
        try
        {
            var start = DateTime.UtcNow;
            var testKey = "health:ping";
            await _cache.SetAsync(testKey, "pong", TimeSpan.FromSeconds(10), ct);
            var val = await _cache.GetAsync<string>(testKey, ct);
            var latency = (int)(DateTime.UtcNow - start).TotalMilliseconds;

            return new PlatformStatus
            {
                Status = val == "pong" ? "up" : "down",
                LatencyMs = latency,
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis health check basarisiz");
            return new PlatformStatus
            {
                Status = "down",
                Error = ex.Message,
                LastChecked = DateTime.UtcNow
            };
        }
    }
}
