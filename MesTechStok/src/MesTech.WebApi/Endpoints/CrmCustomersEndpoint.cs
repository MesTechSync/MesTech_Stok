using MediatR;
using MesTech.Application.Features.Crm.Queries.GetCustomersCrm;
using Microsoft.AspNetCore.OutputCaching;

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

        // GET /api/v1/crm/customers — customer list with search & filter
        group.MapGet("/customers", async (
            ISender mediator,
            Guid tenantId,
            string? search = null,
            bool? isVip = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetCustomersCrmQuery(tenantId, isVip, isActive, search, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetCrmCustomers")
        .WithSummary("CRM müşteri listesi — arama ve segment filtresi (EMR-09)")
        .CacheOutput("Lookup60s");
    }
}
