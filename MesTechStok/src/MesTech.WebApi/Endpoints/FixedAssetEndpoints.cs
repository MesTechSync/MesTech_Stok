using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Accounting;
using MediatR;
using MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;
using MesTech.Application.Features.Accounting.Commands.DeactivateFixedAsset;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedAsset;
using MesTech.Application.Features.Accounting.Queries.CalculateDepreciation;
using MesTech.Application.Features.Accounting.Queries.ListFixedAssets;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class FixedAssetEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounting/fixed-assets")
            .WithTags("Accounting - Fixed Assets")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/accounting/fixed-assets — sabit kiymet listesi
        group.MapGet("/", async (
            Guid tenantId, bool? isActive,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListFixedAssetsQuery(tenantId, isActive), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("ListFixedAssets")
        .WithSummary("Sabit kiymet listesi (aktif/pasif filtresi)").Produces<IReadOnlyList<FixedAssetDto>>(200).Produces(400);

        // GET /api/v1/accounting/fixed-assets/{id}/schedule — amortisman tablosu
        group.MapGet("/{id:guid}/schedule", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new CalculateDepreciationQuery(id), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("CalculateDepreciation")
        .WithSummary("Sabit kiymet amortisman tablosu hesapla (VUK md. 315)").Produces<DepreciationResultDto>(200).Produces(400);

        // POST /api/v1/accounting/fixed-assets — yeni sabit kiymet olustur
        group.MapPost("/", async (
            CreateFixedAssetCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/fixed-assets/{id}", new CreatedResponse(id));
        })
        .WithName("CreateFixedAsset")
        .WithSummary("Yeni sabit kiymet olustur (VUK md. 313)").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // PUT /api/v1/accounting/fixed-assets/{id} — sabit kiymet guncelle
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateFixedAssetCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            await mediator.Send(updated, ct);
            return Results.NoContent();
        })
        .WithName("UpdateFixedAsset")
        .WithSummary("Sabit kiymet bilgilerini guncelle").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // DELETE /api/v1/accounting/fixed-assets/{id} — sabit kiymet pasife al (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeactivateFixedAssetCommand(id, tenantId), ct);
            return Results.NoContent();
        })
        .WithName("DeactivateFixedAsset")
        .WithSummary("Sabit kiymeti pasife al — soft delete").Produces(200).Produces(400);
    }
}
