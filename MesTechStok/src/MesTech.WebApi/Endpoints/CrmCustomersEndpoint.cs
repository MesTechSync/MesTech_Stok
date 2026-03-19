// DEV1-DEPENDENCY: Application CRM Customers CQRS handler not yet created.
// When DEV 1 creates GetCustomersCrmQuery, restore ISender dispatch.

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// CRM Customers endpoint — GET /api/v1/crm/customers.
/// Returns paginated customer list with search and segment filtering.
/// </summary>
public static class CrmCustomersEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/crm")
            .WithTags("CRM")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/crm/customers — customer list with search & segment filter
        group.MapGet("/customers", (
            Guid tenantId,
            string? search,
            string? segment,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default) =>
        {
            // DEV1-DEPENDENCY: GetCustomersCrmQuery not yet available
            // Final: var result = await mediator.Send(
            //   new GetCustomersCrmQuery(tenantId, search, segment, page, pageSize), ct);
            return Results.Ok(new
            {
                TenantId = tenantId,
                Search = search,
                Segment = segment,
                Page = page,
                PageSize = pageSize,
                Items = Array.Empty<object>(),
                TotalCount = 0,
                Message = "CRM Customers endpoint stub — DEV1 Application handler pending"
            });
        })
        .WithName("GetCrmCustomers")
        .WithSummary("CRM müşteri listesi — arama ve segment filtresi (EMR-09)");
    }
}
