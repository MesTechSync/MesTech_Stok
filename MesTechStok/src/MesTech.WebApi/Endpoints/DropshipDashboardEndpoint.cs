using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipDashboard;

namespace MesTech.WebApi.Endpoints;

public static class DropshipDashboardEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/dropship")
            .WithTags("Dropshipping")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/dropship/dashboard — dropship özet panosu
        group.MapGet("/dashboard", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetDropshipDashboardQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetDropshipDashboard")
        .WithSummary("Dropship özet panosu (aktif tedarikçi, ürün, sipariş)")
        .CacheOutput("Dashboard30s");
    }
}
