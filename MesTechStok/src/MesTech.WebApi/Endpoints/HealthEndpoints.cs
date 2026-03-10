using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

namespace MesTech.WebApi.Endpoints;

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
        });

        // GET /metrics — Prometheus text format (no auth — bypass path)
        app.MapGet("/metrics", async (CancellationToken ct) =>
        {
            using var stream = new MemoryStream();
            await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream, ct);
            var metricsText = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            return Results.Text(metricsText, "text/plain; version=0.0.4; charset=utf-8");
        });
    }
}
