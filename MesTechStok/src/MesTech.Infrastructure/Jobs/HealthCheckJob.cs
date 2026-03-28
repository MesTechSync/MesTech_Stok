using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Caching;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Infrastructure.Integration.Health;
using MesTech.Infrastructure.Monitoring;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 15 dakikada tüm platform adapter'larının sağlık durumunu kontrol eder.
/// G099: IPingableAdapter.PingAsync veya GetCategoriesAsync ile gerçek API ping.
/// Sonuçlar: Cache (anlık) + PlatformHealthHistory (24s) + Prometheus (metrik).
/// </summary>
[AutomaticRetry(Attempts = 2)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class HealthCheckJob : ISyncJob
{
    public string JobId => "health-check";
    public string CronExpression => "*/15 * * * *"; // Her 15 dk

    private readonly AdapterHealthService _healthService;
    private readonly PlatformHealthHistory _history;
    private readonly ICacheService _cache;
    private readonly ILogger<HealthCheckJob> _logger;

    public HealthCheckJob(
        AdapterHealthService healthService,
        PlatformHealthHistory history,
        ICacheService cache,
        ILogger<HealthCheckJob> logger)
    {
        _healthService = healthService;
        _history = history;
        _cache = cache;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Platform health check başlıyor...", JobId);

        var report = await _healthService.CheckAllAdaptersAsync(ct).ConfigureAwait(false);

        foreach (var adapter in report.Adapters)
        {
            // 1. PlatformHealthHistory — 24h circular buffer
            _history.Record(adapter.PlatformCode, adapter.IsHealthy, adapter.ResponseTimeMs);

            // 2. Prometheus metrics
            AdapterMetrics.ApiCallsTotal
                .WithLabels(adapter.PlatformCode.ToLowerInvariant(), "health_check",
                    adapter.IsHealthy ? "success" : "error")
                .Inc();

            AdapterMetrics.ApiCallDuration
                .WithLabels(adapter.PlatformCode.ToLowerInvariant(), "health_check")
                .Observe(adapter.ResponseTimeMs / 1000.0);
        }

        // 3. Cache — anlık durum
        await _cache.SetAsync(CacheKeys.Health("all"), report, CacheKeys.HealthTTL, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[{JobId}] Health check tamamlandı: {Healthy}/{Total} healthy, {Unhealthy} unhealthy",
            JobId, report.HealthyCount, report.TotalAdapters, report.UnhealthyCount);
    }
}
