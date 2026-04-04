using System.Diagnostics;
using System.Reflection;
using MediatR;
using MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;
using MesTech.Application.Features.System.Kvkk.Queries.ExportPersonalData;
using MesTech.Application.Features.System.Kvkk.Queries.GetKvkkAuditLogs;
using MesTech.Application.Features.System.LaunchReadiness;
using MesTech.Application.Features.System.Users;
using MesTech.Infrastructure.Integration.Health;
using MesTech.Infrastructure.Jobs;

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

            return Results.Ok(new SystemStatusResponse(
                "MesTech.WebApi",
                version,
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                new UptimeInfo((int)uptime.TotalSeconds,
                    $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s"),
                new MemoryInfo(
                    Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 1),
                    Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 1),
                    GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)),
                new RuntimeInfo(
                    Environment.Version.ToString(),
                    Environment.OSVersion.ToString(),
                    Environment.ProcessorCount),
                DateTime.UtcNow));
        })
        .WithName("GetSystemStatus")
        .WithSummary("Sistem durumu (uptime, versiyon, bellek bilgisi)").Produces<SystemStatusResponse>(200).Produces(400);

        // GET /api/v1/admin/system/jobs — recurring job dashboard
        group.MapGet("/jobs", (HangfireJobMonitorService monitor) =>
            Results.Ok(monitor.GetDashboard()))
        .WithName("GetBackgroundJobs")
        .WithSummary("Hangfire recurring job dashboard — status, lastExec, nextExec, cron").Produces<HangfireJobDashboard>(200).Produces(400);

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
        .WithSummary("Production launch hazirlik raporu — 26 kriter").Produces<LaunchReadinessDto>(200).Produces(400);

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
        .WithSummary("KVKK — kişisel veri silme talebi").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

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
        .WithSummary("KVKK — kişisel veri dışa aktarma").Produces<PersonalDataExportDto>(200).Produces(400);

        // GET /api/v1/admin/system/kvkk/audit-logs — KVKK denetim kayıtları
        group.MapGet("/kvkk/audit-logs", async (
            Guid tenantId, int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetKvkkAuditLogsQuery(tenantId, Math.Max(1, page), Math.Clamp(pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetKvkkAuditLogs")
        .WithSummary("KVKK — denetim kayıtları (yasal saklama 10 yıl)").Produces<KvkkAuditLogsResult>(200).Produces(400);

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
        .WithSummary("Kullanıcı listesi (tenant bazlı veya tümü)").Produces<IReadOnlyList<UserListItemDto>>(200).Produces(400);

        // GET /api/v1/admin/system/adapter-health — platform adapter ping durumu
        group.MapGet("/adapter-health", async (
            MesTech.Infrastructure.Integration.Health.AdapterHealthService healthService,
            CancellationToken ct) =>
        {
            var report = await healthService.CheckAllAdaptersAsync(ct);
            return Results.Ok(report);
        })
        .WithName("GetAdapterHealth")
        .WithSummary("Tüm platform adapter'larının bağlantı durumu (parallel PingAsync)").Produces<AdapterHealthReport>(200).Produces(400);
    }

    public sealed record SystemStatusResponse(
        string Application, string Version, string Environment,
        UptimeInfo Uptime, MemoryInfo Memory, RuntimeInfo Runtime, DateTime Timestamp);
    public sealed record UptimeInfo(int TotalSeconds, string Formatted);
    public sealed record MemoryInfo(
        double WorkingSetMb, double GcTotalMemoryMb,
        int Gen0Collections, int Gen1Collections, int Gen2Collections);
    public sealed record RuntimeInfo(string DotNetVersion, string OsPlatform, int ProcessorCount);
}
