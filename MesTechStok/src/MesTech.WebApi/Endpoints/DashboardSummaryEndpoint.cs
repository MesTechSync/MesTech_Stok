using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// GET /api/v1/dashboard/summary — Unified 12-KPI dashboard özeti.
/// Mevcut DashboardEndpoints.cs dosyasına DOKUNMAZ (5 mevcut endpoint korunur).
/// </summary>
public static class DashboardSummaryEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/v1/dashboard/summary", async (
            Guid tenantId,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetDashboardSummaryQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .RequireRateLimiting("PerApiKey")
        .WithTags("Dashboard")
        .WithName("GetDashboardSummary")
        .WithSummary("Unified dashboard özeti — 12 KPI, 7 günlük satış grafiği, platform dağılımı, son siparişler, kritik stok");
    }
}
