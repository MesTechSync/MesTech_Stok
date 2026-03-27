using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.Features.Crm.Queries.GetCrmDashboard;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// CRM Dashboard endpoint — GET /api/v1/crm/dashboard.
/// Returns aggregated CRM metrics: lead/deal counts, conversion rates, revenue pipeline.
/// </summary>
public static class CrmDashboardEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/crm")
            .WithTags("CRM")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/crm/dashboard — CRM dashboard KPI summary
        group.MapGet("/dashboard", async (
            ISender mediator,
            Guid tenantId,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetCrmDashboardQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetCrmDashboard")
        .WithSummary("CRM dashboard — lead/deal/pipeline metrikleri (EMR-09)")
        .Produces(200)
        .CacheOutput("Dashboard30s");
    }
}
