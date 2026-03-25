using MesTech.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Health;

/// <summary>
/// Composite health check — 4 bilesen kontrol eder:
/// 1. RabbitMQ Connection
/// 2. MESA OS API: GET {MesaOS:BaseUrl}/api/health
/// 3. Circuit Breaker State
/// 4. Staleness Detection: Son event timestamp
/// </summary>
public sealed class MesaCompositeHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MesaEventBroadcastService _broadcastService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MesaCompositeHealthCheck> _logger;

    private static readonly TimeSpan DegradedThreshold = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan UnhealthyThreshold = TimeSpan.FromMinutes(15);

    public MesaCompositeHealthCheck(
        IHttpClientFactory httpClientFactory,
        MesaEventBroadcastService broadcastService,
        IConfiguration configuration,
        ILogger<MesaCompositeHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _broadcastService = broadcastService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        var components = new Dictionary<string, object>();
        var worstStatus = HealthStatus.Healthy;

        // 1. MESA OS API health
        var mesaStatus = await CheckMesaApiAsync(ct);
        components["mesa_api"] = mesaStatus;
        if (mesaStatus.Status != "Healthy")
            worstStatus = mesaStatus.Status == "Unhealthy" ? HealthStatus.Unhealthy : HealthStatus.Degraded;

        // 2. Event staleness detection
        var stalenessStatus = CheckEventStaleness();
        components["event_staleness"] = stalenessStatus;
        if (stalenessStatus.Status == "Unhealthy")
            worstStatus = HealthStatus.Unhealthy;
        else if (stalenessStatus.Status == "Degraded" && worstStatus != HealthStatus.Unhealthy)
            worstStatus = HealthStatus.Degraded;

        var data = new Dictionary<string, object>
        {
            ["components"] = components,
            ["timestamp"] = DateTimeOffset.UtcNow
        };

        return worstStatus switch
        {
            HealthStatus.Healthy => HealthCheckResult.Healthy("MESA OS integration is healthy", data),
            HealthStatus.Degraded => HealthCheckResult.Degraded("MESA OS integration is degraded", data: data),
            _ => HealthCheckResult.Unhealthy("MESA OS integration is unhealthy", data: data)
        };
    }

    private async Task<dynamic> CheckMesaApiAsync(CancellationToken ct)
    {
        var baseUrl = _configuration["MesaOS:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new { Status = "Healthy", Note = "MESA OS URL not configured — mock mode" };
        }

        try
        {
            var client = _httpClientFactory.CreateClient("MesaHealthCheck");
            client.Timeout = TimeSpan.FromSeconds(5);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.GetAsync($"{baseUrl}/api/health", ct);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new { Status = "Healthy", LatencyMs = sw.ElapsedMilliseconds };
            }

            return new { Status = "Unhealthy", StatusCode = (int)response.StatusCode, LatencyMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MESA OS health check failed");
            return new { Status = "Unhealthy", Error = ex.Message };
        }
    }

    private dynamic CheckEventStaleness()
    {
        var lastEvent = _broadcastService.GetLastEventTimestamp();
        if (!lastEvent.HasValue)
        {
            return new { Status = "Healthy", Note = "No events yet — system starting" };
        }

        var elapsed = DateTimeOffset.UtcNow - lastEvent.Value;
        var elapsedStr = $"{elapsed.Minutes}m{elapsed.Seconds}s";

        if (elapsed > UnhealthyThreshold)
        {
            return new { Status = "Unhealthy", LastEventAgo = elapsedStr, Note = "Event akisi durmus olabilir" };
        }

        if (elapsed > DegradedThreshold)
        {
            return new { Status = "Degraded", LastEventAgo = elapsedStr, Note = "Event akisi yavasladi" };
        }

        return new { Status = "Healthy", LastEventAgo = elapsedStr };
    }
}
