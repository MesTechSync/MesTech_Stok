using System.Diagnostics;
using System.Reflection;
using MediatR;
using MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;
using MesTech.Application.Features.System.Kvkk.Queries.ExportPersonalData;
using MesTech.Application.Features.System.Kvkk.Queries.GetKvkkAuditLogs;
using MesTech.Application.Features.System.LaunchReadiness;
using MesTech.Application.Features.System.Users;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Admin-only system health endpoints — uptime, version, memory, and job status.
/// </summary>
public static class SystemHealthEndpoints
{
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/system")
            .WithTags("System (Admin)")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/admin/system/status — sistem durumu
        group.MapGet("/status", () =>
        {
            var process = Process.GetCurrentProcess();
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "0.0.0";
            var uptime = DateTime.UtcNow - StartTime;

            return Results.Ok(new
            {
                Application = "MesTech.WebApi",
                Version = version,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                Uptime = new
                {
                    TotalSeconds = (int)uptime.TotalSeconds,
                    Formatted = $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s"
                },
                Memory = new
                {
                    WorkingSetMb = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 1),
                    GcTotalMemoryMb = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 1),
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2)
                },
                Runtime = new
                {
                    DotNetVersion = Environment.Version.ToString(),
                    OsPlatform = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount
                },
                Timestamp = DateTime.UtcNow
            });
        })
        .WithName("GetSystemStatus")
        .WithSummary("Sistem durumu (uptime, versiyon, bellek bilgisi)");

        // GET /api/v1/admin/system/jobs — arka plan iş listesi
        // DEV4-DEPENDENCY: Hangfire entegrasyonu henüz yok
        group.MapGet("/jobs", () =>
            Results.Ok(new
            {
                Message = "Background jobs endpoint — Hangfire integration pending (DEV4-DEPENDENCY)",
                Jobs = Array.Empty<object>(),
                Status = "not_implemented"
            }))
        .WithName("GetBackgroundJobs")
        .WithSummary("Arka plan iş listesi — Hangfire (DEV4-DEPENDENCY)");

        // GET /api/v1/admin/system/launch-readiness — canliya cikis hazirlik raporu
        group.MapGet("/launch-readiness", async (
            Guid tenantId,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetLaunchReadinessQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetLaunchReadiness")
        .WithSummary("Production launch hazirlik raporu — 26 kriter");

        // ─── DEFTER KAPATMA: KVKK + Users endpoint [ENT-DEV6] ───

        // POST /api/v1/admin/system/kvkk/delete — kişisel veri silme (KVKK hakkı)
        group.MapPost("/kvkk/delete", async (
            DeletePersonalDataCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("DeletePersonalData")
        .WithSummary("KVKK — kişisel veri silme talebi");

        // GET /api/v1/admin/system/kvkk/export — kişisel veri dışa aktarma (KVKK hakkı)
        group.MapGet("/kvkk/export", async (
            Guid tenantId, Guid requestedByUserId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ExportPersonalDataQuery(tenantId, requestedByUserId), ct);
            return Results.Ok(result);
        })
        .WithName("ExportPersonalData")
        .WithSummary("KVKK — kişisel veri dışa aktarma");

        // GET /api/v1/admin/system/kvkk/audit-logs — KVKK denetim kayıtları
        group.MapGet("/kvkk/audit-logs", async (
            Guid tenantId, int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetKvkkAuditLogsQuery(tenantId, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetKvkkAuditLogs")
        .WithSummary("KVKK — denetim kayıtları (yasal saklama 10 yıl)");

        // GET /api/v1/admin/system/users — kullanıcı listesi
        group.MapGet("/users", async (
            Guid? tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetUsersQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetUsers")
        .WithSummary("Kullanıcı listesi (tenant bazlı veya tümü)");

        // GET /api/v1/admin/system/adapter-health — platform adapter ping durumu
        group.MapGet("/adapter-health", async (
            MesTech.Infrastructure.Integration.Health.AdapterHealthService healthService,
            CancellationToken ct) =>
        {
            var report = await healthService.CheckAllAdaptersAsync(ct);
            return Results.Ok(report);
        })
        .WithName("GetAdapterHealth")
        .WithSummary("Tüm platform adapter'larının bağlantı durumu (parallel PingAsync)");
    }
}
