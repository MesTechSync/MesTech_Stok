using MediatR;
using MesTech.Application.Features.Platform.Queries.GetPlatformList;

namespace MesTech.WebApi.Endpoints;

public static class PlatformListEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/platforms")
            .WithTags("Platforms")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/platforms — kiracıya ait platform listesi
        group.MapGet("/", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetPlatformListQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetPlatformList")
        .WithSummary("Kiracıya ait platform kartları listesi");
    }
}
