using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.HealthChecks;

/// <summary>
/// MESA OS baglanti kontrolu.
/// GET http://localhost:{Mesa:BaseUrl}/health endpoint'ini sorgular.
/// MESA OS erisilemezse Degraded doner — MesTech calismaya devam eder (mock modunda).
/// </summary>
public sealed class MesaOSHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MesaOSHealthCheck> _logger;

    public MesaOSHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MesaOSHealthCheck> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MesaOSHealth");
        _logger = logger;

        var baseUrl = configuration["Mesa:BaseUrl"] ?? "http://localhost:3000";
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("health", ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("[MESA Health] MESA OS erisilebilir");
                return HealthCheckResult.Healthy("MESA OS erisilebilir");
            }

            _logger.LogWarning(
                "[MESA Health] MESA OS yanit verdi ama sagliksiz: {StatusCode}",
                response.StatusCode);
            return HealthCheckResult.Degraded(
                $"MESA OS yanit verdi ama sagliksiz: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "[MESA Health] MESA OS erisilemedi — mock modunda calisiliyor");
            return HealthCheckResult.Degraded(
                "MESA OS erisilemedi — mock modunda calisiliyor",
                ex);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("[MESA Health] MESA OS timeout — mock modunda calisiliyor");
            return HealthCheckResult.Degraded(
                "MESA OS timeout — mock modunda calisiliyor");
        }
    }
}
