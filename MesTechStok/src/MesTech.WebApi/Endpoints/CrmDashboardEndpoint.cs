// DEV1-DEPENDENCY: Application CRM Dashboard CQRS handler not yet created.
// When DEV 1 creates GetCrmDashboardQuery, restore ISender dispatch.

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
        group.MapGet("/dashboard", (Guid tenantId, CancellationToken ct) =>
        {
            // DEV1-DEPENDENCY: GetCrmDashboardQuery not yet available
            // Final: var result = await mediator.Send(new GetCrmDashboardQuery(tenantId), ct);
            return Results.Ok(new
            {
                TenantId = tenantId,
                TotalLeads = 0,
                TotalDeals = 0,
                OpenDeals = 0,
                WonDeals = 0,
                LostDeals = 0,
                ConversionRate = 0.0m,
                PipelineValue = 0.0m,
                RevenueThisMonth = 0.0m,
                ActiveCustomers = 0,
                UnreadMessages = 0,
                Message = "CRM Dashboard endpoint stub — DEV1 Application handler pending"
            });
        })
        .WithName("GetCrmDashboard")
        .WithSummary("CRM dashboard — lead/deal/pipeline metrikleri (EMR-09)");
    }
}
