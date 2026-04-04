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
        .AllowAnonymous()
        .RequireRateLimiting("HealthRateLimit")
        .Produces(200).Produces(503);

        // GET /health/deep — infra + adapter ping + cargo ping + MESA (G054 + G439)
        app.MapGet("/health/deep", async (
            HealthCheckService healthCheckService,
            IAdapterFactory adapterFactory,
            ICargoProviderFactory cargoProviderFactory,
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
                    logger.LogWarning("Adapter {Platform} does not implement IPingableAdapter — health status unknown, reporting degraded", adapter.PlatformCode);
                    return new HealthCheckItem(name, false, adapterSw.Elapsed.TotalMilliseconds, "No IPingableAdapter — degraded");
                }
                catch (Exception ex)
                {
                    adapterSw.Stop();
                    logger.LogWarning(ex, "Adapter {Platform} health ping failed", adapter.PlatformCode);
                    return new HealthCheckItem(name, false, adapterSw.Elapsed.TotalMilliseconds, "Connection failed");
                }
            });
            adapterChecks.AddRange(await Task.WhenAll(pingTasks));

            // 2b. Cargo adapter ping (parallel) — G439
            var cargoChecks = new List<HealthCheckItem>();
            var cargoAdapters = cargoProviderFactory.GetAll();
            var cargoPingTasks = cargoAdapters.Select(async cargo =>
            {
                var name = $"cargo:{cargo.Provider}";
                var cargoSw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(5));
                    var ok = await cargo.PingAsync(cts.Token);
                    cargoSw.Stop();
                    return new HealthCheckItem(name, ok, cargoSw.Elapsed.TotalMilliseconds, ok ? null : "Cargo ping failed");
                }
                catch (Exception)
                {
                    cargoSw.Stop();
                    return new HealthCheckItem(name, false, cargoSw.Elapsed.TotalMilliseconds, "Cargo connection failed");
                }
            });
            cargoChecks.AddRange(await Task.WhenAll(cargoPingTasks));

            // 3. MESA OS status
            HealthCheckItem mesaCheck;
            var mesaUrl = configuration["Mesa:BaseUrl"] ?? "https://mestech-mesa:3105";
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
                logger.LogWarning(ex, "MESA OS health check failed at {MesaUrl}", mesaUrl);
                mesaCheck = new HealthCheckItem("mesa-os", false, 0, "Connection failed");
            }

            sw.Stop();

            var allChecks = infraChecks.Concat(adapterChecks).Concat(cargoChecks).Append(mesaCheck).ToList();
            var allHealthy = allChecks.All(c => c.IsHealthy);

            var result = new
            {
                status = allHealthy ? "healthy" : "degraded",
                timestamp = DateTime.UtcNow,
                totalDurationMs = sw.Elapsed.TotalMilliseconds,
                infrastructure = infraChecks,
                adapters = adapterChecks,
                cargo = cargoChecks,
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
        .WithTags("Health")
        .RequireRateLimiting("HealthRateLimit")
        .Produces(200).Produces(503);

        // GET /health/platforms — platform sağlık özeti (G166-DEV6)
        app.MapGet("/health/platforms", (
            MesTech.Application.Interfaces.IPlatformHealthProvider healthProvider) =>
        {
            var summaries = healthProvider.GetAllHealthSummaries();
            var allHealthy = summaries.All(s => s.FailedChecks24h == 0);
            return Results.Json(new
            {
                status = allHealthy ? "healthy" : "degraded",
                timestamp = DateTime.UtcNow,
                platforms = summaries.Select(s => new
                {
                    platform = s.PlatformCode,
                    lastCheckUtc = s.LastCheckUtc,
                    uptimePercent24h = s.UptimePercent24h,
                    failedChecks24h = s.FailedChecks24h,
                    avgResponseTimeMs = s.AvgResponseTimeMs,
                    totalChecks24h = s.TotalChecks24h
                }),
                summary = new
                {
                    total = summaries.Count,
                    healthy = summaries.Count(s => s.FailedChecks24h == 0),
                    degraded = summaries.Count(s => s.FailedChecks24h > 0)
                }
            });
        })
        .WithName("PlatformHealth")
        .WithSummary("Platform sağlık özeti — HealthCheckJob 15dk verisi (G166)")
        .WithTags("Health")
        .RequireRateLimiting("HealthRateLimit")
        .Produces(200);

        // GET /health/ready — Kubernetes readiness probe (DB + Redis bağlı mı?)
        app.MapGet("/health/ready", async (HealthCheckService healthCheckService, CancellationToken ct) =>
        {
            var report = await healthCheckService.CheckHealthAsync(
                registration => registration.Tags.Contains("ready"), ct);
            var statusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
            return Results.Json(new
            {
                status = report.Status.ToString().ToLowerInvariant(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString().ToLowerInvariant()
                })
            }, statusCode: statusCode);
        })
        .WithName("ReadinessCheck")
        .WithSummary("Kubernetes readiness probe — kritik altyapı kontrolleri")
        .WithTags("Health")
        .AllowAnonymous()
        .RequireRateLimiting("HealthRateLimit")
        .Produces(200).Produces(503);

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
        .AllowAnonymous()
        .RequireRateLimiting("HealthRateLimit")
        .Produces(200);

        // GET /api/health/adapters — adapter connectivity report (G10800-DEV6)
        app.MapGet("/api/health/adapters", async (
            MesTech.Infrastructure.Integration.Orchestration.AdapterConnectivityValidator validator,
            CancellationToken ct) =>
        {
            var report = await validator.ValidateAllAsync(ct);
            return Results.Ok(new AdapterConnectivityResponse(
                report.TotalCount, report.ReachableCount, report.UnreachableCount,
                report.AllReachable, report.TotalElapsed.TotalMilliseconds,
                report.Results.Select(r => new AdapterPingResponse(
                    r.PlatformCode, r.IsReachable,
                    r.ResponseTime.TotalMilliseconds, r.Error)).ToList()));
        })
        .WithName("GetAdapterConnectivity")
        .WithSummary("Adapter connectivity report — all platform ping results (G10800)")
        .WithTags("Health")
        .RequireAuthorization()
        .RequireRateLimiting("HealthRateLimit")
        .Produces(200)
        .CacheOutput("Dashboard30s");
    }

    private sealed record HealthCheckItem(string Name, bool IsHealthy, double DurationMs, string? Error);
    public sealed record AdapterConnectivityResponse(
        int TotalAdapters, int Reachable, int Unreachable,
        bool AllHealthy, double TotalElapsedMs,
        IReadOnlyList<AdapterPingResponse> Adapters);
    public sealed record AdapterPingResponse(
        string Platform, bool IsReachable, double ResponseTimeMs, string? Error);
}
