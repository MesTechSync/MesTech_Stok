using MediatR;
using MesTech.Application.Features.Crm.Commands.EarnPoints;
using MesTech.Application.Features.Crm.Commands.RedeemPoints;
using MesTech.Application.Features.Crm.Queries.GetCustomerPoints;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class LoyaltyEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/loyalty")
            .WithTags("Loyalty Program")
            .RequireRateLimiting("PerApiKey");

        group.MapPost("/earn", async (
            EarnPointsCommand cmd,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return Results.Ok(result);
        })
        .WithName("EarnPoints")
        .WithSummary("Musteriye puan kazan");

        group.MapPost("/redeem", async (
            RedeemPointsCommand cmd,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return Results.Ok(result);
        })
        .WithName("RedeemPoints")
        .WithSummary("Musteri puani kullan");

        group.MapGet("/{tenantId:guid}/{customerId:guid}", async (
            Guid tenantId,
            Guid customerId,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCustomerPointsQuery(tenantId, customerId), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetCustomerPoints")
        .WithSummary("Musteri puan bakiyesi");
    }
}
