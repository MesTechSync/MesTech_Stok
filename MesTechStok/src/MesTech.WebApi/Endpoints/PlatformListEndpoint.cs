using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.Features.Platform.Queries.GetPlatformList;
using MesTech.Application.Features.Platform.Queries.GetPlatformDashboard;
using MesTech.Domain.Enums;

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
        .WithSummary("Kiracıya ait platform kartları listesi")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/platforms/{platform}/dashboard — generic marketplace dashboard (G413-DEV6)
        group.MapGet("/{platform}/dashboard", async (
            PlatformType platform,
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetPlatformDashboardQuery(tenantId, platform), ct);
            return Results.Ok(result);
        })
        .WithName("GetPlatformDashboard")
        .WithSummary("Generic marketplace dashboard — 15 platform KPI (G413)")
        .Produces(200).Produces(400)
        .CacheOutput("Dashboard30s");
    }
}
