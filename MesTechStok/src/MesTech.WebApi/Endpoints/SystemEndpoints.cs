using System.Threading.RateLimiting;
using MediatR;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Application.Features.System.Queries.GetBackupHistory;

namespace MesTech.WebApi.Endpoints;

public static class SystemEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/system")
            .WithTags("System")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/system/audit-logs — erişim/denetim logları
        group.MapGet("/audit-logs", async (
            Guid tenantId,
            DateTime? from = null, DateTime? to = null,
            string? userId = null, string? action = null,
            int page = 1, int pageSize = 50,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetAuditLogsQuery(tenantId, from, to, userId, action, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetAuditLogs")
        .WithSummary("Erişim ve denetim logları (tarih/kullanıcı/aksiyon filtresi)");

        // GET /api/v1/system/backups — yedek geçmişi
        group.MapGet("/backups", async (
            Guid tenantId, int limit = 20,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetBackupHistoryQuery(tenantId, limit), ct);
            return Results.Ok(result);
        })
        .WithName("GetBackupHistory")
        .WithSummary("Yedekleme geçmişi (son N kayıt)");

        // GET /api/v1/system/rate-limit-status — API kota durumu
        group.MapGet("/rate-limit-status", (HttpContext httpContext) =>
        {
            var rateLimitFeature = httpContext.Features
                .Get<RateLimitLease>();

            return Results.Ok(new
            {
                Limit = 100,
                WindowSeconds = 60,
                Policy = "PerApiKey",
                Description = "100 request per minute per API key"
            });
        })
        .WithName("GetRateLimitStatus")
        .WithSummary("API rate limit kota bilgisi");
    }
}
