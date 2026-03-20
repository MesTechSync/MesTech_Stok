using MediatR;
using MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;
using MesTech.Application.Features.Accounting.Queries.CalculateDepreciation;

namespace MesTech.WebApi.Endpoints;

public static class FixedAssetEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounting/fixed-assets")
            .WithTags("Accounting - Fixed Assets")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/accounting/fixed-assets — sabit kiymet listesi
        // Awaiting DEV-1 ListFixedAssetsQuery handler
        group.MapGet("/", async (
            Guid tenantId, bool? isActive,
            ISender mediator, CancellationToken ct) =>
        {
            // var result = await mediator.Send(
            //     new ListFixedAssetsQuery(tenantId, isActive ?? true), ct);
            // return Results.Ok(result);
            return Results.StatusCode(StatusCodes.Status501NotImplemented);
        })
        .WithName("ListFixedAssets")
        .WithSummary("Sabit kiymet listesi (aktif/pasif filtresi — DEV-1 handler bekleniyor)");

        // GET /api/v1/accounting/fixed-assets/{id}/schedule — amortisman tablosu
        group.MapGet("/{id:guid}/schedule", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new CalculateDepreciationQuery(id), ct);
            return Results.Ok(result);
        })
        .WithName("CalculateDepreciation")
        .WithSummary("Sabit kiymet amortisman tablosu hesapla (VUK md. 315)");

        // POST /api/v1/accounting/fixed-assets — yeni sabit kiymet olustur
        group.MapPost("/", async (
            CreateFixedAssetCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/fixed-assets/{id}", new { id });
        })
        .WithName("CreateFixedAsset")
        .WithSummary("Yeni sabit kiymet olustur (VUK md. 313)");

        // PUT /api/v1/accounting/fixed-assets/{id} — sabit kiymet guncelle
        // Awaiting DEV-1 UpdateFixedAssetCommand handler
        group.MapPut("/{id:guid}", async (
            Guid id,
            // UpdateFixedAssetCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            // var updated = command with { Id = id };
            // await mediator.Send(updated, ct);
            // return Results.NoContent();
            return Results.StatusCode(StatusCodes.Status501NotImplemented);
        })
        .WithName("UpdateFixedAsset")
        .WithSummary("Sabit kiymet bilgilerini guncelle (DEV-1 handler bekleniyor)");

        // DELETE /api/v1/accounting/fixed-assets/{id} — sabit kiymet pasife al (soft delete)
        // Awaiting DEV-1 DeactivateFixedAssetCommand handler
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            // await mediator.Send(new DeactivateFixedAssetCommand(id), ct);
            // return Results.NoContent();
            return Results.StatusCode(StatusCodes.Status501NotImplemented);
        })
        .WithName("DeactivateFixedAsset")
        .WithSummary("Sabit kiymeti pasife al — soft delete (DEV-1 handler bekleniyor)");
    }
}
