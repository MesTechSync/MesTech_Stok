using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Crm.Commands.CreateCampaign;
using MesTech.Application.Features.Crm.Commands.DeactivateCampaign;
using MesTech.Application.Features.Crm.Queries.ApplyCampaignDiscount;
using MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class CampaignEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/campaigns")
            .WithTags("Campaigns")
            .RequireRateLimiting("PerApiKey");

        group.MapPost("/", async (
            CreateCampaignCommand cmd,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return Results.Created($"/api/v1/campaigns/{result}", new { id = result });
        })
        .WithName("CreateCampaign")
        .WithSummary("Yeni kampanya olustur");

        group.MapGet("/", async (
            Guid tenantId,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetActiveCampaignsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("ListCampaigns")
        .WithSummary("Aktif kampanya listesi");

        group.MapGet("/discount", async (
            Guid productId,
            decimal price,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new ApplyCampaignDiscountQuery(productId, price), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("CalculateDiscount")
        .WithSummary("Urun icin kampanya indirimi hesapla");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new DeactivateCampaignCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeactivateCampaign")
        .WithSummary("Kampanyayi pasife al");
    }
}
