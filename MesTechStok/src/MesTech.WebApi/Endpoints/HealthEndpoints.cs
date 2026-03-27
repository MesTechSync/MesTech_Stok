using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Health + Metrics endpoints.
/// G054-DEV6: Genişletilmiş health — infra + adapter ping + MESA status.
/// </summary>
public static class HealthEndpoints
{
    public static void Map(WebApplication app)
    {
        // GET /health — JSON health status (no auth — bypass path)
        app.MapGet("/health", async (HealthCheckService healthCheckService, CancellationToken ct) =>
        {
            var report = await healthCheckService.CheckHealthAsync(ct);
            var result = new
            {
                status = report.Status.ToString().ToLowerInvariant(),
                timestamp = DateTime.UtcNow,
                duration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString().ToLowerInvariant(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                    error = e.Value.Exception?.Message
                })
            };

            var statusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
            return Results.Json(result, statusCode: statusCode);
        })
        .WithName("HealthCheck")
        .WithSummary("Sistem sağlık durumu — PostgreSQL, Redis, RabbitMQ, MinIO")
        .WithTags("Health")
        .AllowAnonymous();

        // GET /health/deep — infra + adapter ping + MESA (G054)
        app.MapGet("/health/deep", async (
            HealthCheckService healthCheckService,
            IAdapterFactory adapterFactory,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.HealthDeep");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // 1. Infrastructure health (PG, Redis, RabbitMQ, MinIO)
            var infraReport = await healthCheckService.CheckHealthAsync(ct);
            var infraChecks = infraReport.Entries.Select(e => new HealthCheckItem(
                e.Key, e.Value.Status == HealthStatus.Healthy, e.Value.Duration.TotalMilliseconds,
                e.Value.Exception?.Message));

            // 2. Platform adapter ping (parallel)
            var adapterChecks = new List<HealthCheckItem>();
            var adapters = adapterFactory.GetAll();
            var pingTasks = adapters.Select(async adapter =>
            {
                var name = $"adapter:{adapter.PlatformCode}";
                var adapterSw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    if (adapter is IPingableAdapter pingable)
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        cts.CancelAfter(TimeSpan.FromSeconds(5));
                        var ok = await pingable.PingAsync(cts.Token);
                        adapterSw.Stop();
                        return new HealthCheckItem(name, ok, adapterSw.Elapsed.TotalMilliseconds, ok ? null : "Ping failed");
                    }
                    adapterSw.Stop();
                    return new HealthCheckItem(name, true, adapterSw.Elapsed.TotalMilliseconds, "No IPingableAdapter");
                }
                catch (Exception ex)
                {
                    adapterSw.Stop();
                    return new HealthCheckItem(name, false, adapterSw.Elapsed.TotalMilliseconds, "Connection failed");
                }
            });
            adapterChecks.AddRange(await Task.WhenAll(pingTasks));

            // 3. MESA OS status
            HealthCheckItem mesaCheck;
            var mesaUrl = configuration["Mesa:BaseUrl"] ?? "http://localhost:3105";
            try
            {
                using var http = httpClientFactory.CreateClient("MesaHealth");
                http.Timeout = TimeSpan.FromSeconds(3);
                var mesaSw = System.Diagnostics.Stopwatch.StartNew();
                var resp = await http.GetAsync($"{mesaUrl}/api/mesa/status", ct);
                mesaSw.Stop();
                mesaCheck = new HealthCheckItem("mesa-os", resp.IsSuccessStatusCode, mesaSw.Elapsed.TotalMilliseconds,
                    resp.IsSuccessStatusCode ? null : $"HTTP {(int)resp.StatusCode}");
            }
            catch (Exception ex)
            {
                mesaCheck = new HealthCheckItem("mesa-os", false, 0, "Connection failed");
            }

            sw.Stop();

            var allChecks = infraChecks.Concat(adapterChecks).Append(mesaCheck).ToList();
            var allHealthy = allChecks.All(c => c.IsHealthy);

            var result = new
            {
                status = allHealthy ? "healthy" : "degraded",
                timestamp = DateTime.UtcNow,
                totalDurationMs = sw.Elapsed.TotalMilliseconds,
                infrastructure = infraChecks,
                adapters = adapterChecks,
                mesa = mesaCheck,
                summary = new
                {
                    total = allChecks.Count,
                    healthy = allChecks.Count(c => c.IsHealthy),
                    unhealthy = allChecks.Count(c => !c.IsHealthy)
                }
            };

            return Results.Json(result, statusCode: allHealthy ? 200 : 503);
        })
        .WithName("DeepHealthCheck")
        .WithSummary("Derin sağlık kontrolü — infra + platform adapter ping + MESA OS (G054)")
        .WithTags("Health");

        // GET /metrics — Prometheus text format (no auth — bypass path)
        app.MapGet("/metrics", async (CancellationToken ct) =>
        {
            using var stream = new MemoryStream();
            await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream, ct);
            var metricsText = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            return Results.Text(metricsText, "text/plain; version=0.0.4; charset=utf-8");
        })
        .WithName("PrometheusMetrics")
        .WithSummary("Prometheus metrikleri — text/plain format")
        .WithTags("Health")
        .AllowAnonymous();
    }

    private sealed record HealthCheckItem(string Name, bool IsHealthy, double DurationMs, string? Error);
}
