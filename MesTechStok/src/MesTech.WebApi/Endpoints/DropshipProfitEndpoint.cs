using MediatR;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProfitability;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class DropshipProfitEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/dropship")
            .WithTags("Dropshipping")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/dropship/profitability — dropship kârlılık raporu
        group.MapGet("/profitability", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetDropshipProfitabilityQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetDropshipProfitability")
        .WithSummary("Dropship kârlılık raporu")
        .CacheOutput("Report120s");
    }
}
