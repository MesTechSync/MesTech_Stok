using MediatR;
using MesTech.Application.Commands.MapProductToPlatform;
using MesTech.Application.Commands.SyncCiceksepetiProducts;
using MesTech.Application.Commands.SyncHepsiburadaProducts;
using MesTech.Application.Commands.SyncN11Products;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.Commands.SyncTrendyolProducts;
using MesTech.Application.Features.Platform.Commands.TriggerSync;
using MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;
using MesTech.Application.Features.Platform.Queries.GetSyncHistory;
using MesTech.Domain.Enums;

namespace MesTech.WebApi.Endpoints;

public static class PlatformSyncEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/platforms")
            .WithTags("Platforms")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/platforms/sync-status — platform senkronizasyon durumları
        group.MapGet("/sync-status", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetPlatformSyncStatusQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetPlatformSyncStatus")
        .WithSummary("Platform senkronizasyon durum listesi").Produces(200).Produces(400)
        .CacheOutput("Dashboard30s");

        // POST /api/v1/platforms/{platformCode}/sync — platform senkronizasyonu başlat
        group.MapPost("/{platformCode}/sync", async (
            string platformCode,
            SyncDirection? direction,
            DateTime? since,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new SyncPlatformCommand(platformCode, direction ?? SyncDirection.Bidirectional, since), ct);
            return Results.Ok(result);
        })
        .WithName("SyncPlatform")
        .WithSummary("Belirtilen platformu senkronize et (* = tümü)").Produces(200).Produces(400);

        // POST /api/v1/platforms/trigger-sync — trigger sync for specific platform
        group.MapPost("/trigger-sync", async (
            TriggerSyncCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("TriggerSync")
        .WithSummary("Platform senkronizasyonu tetikle (tenant + platform kodu)")
        .Produces(200).Produces(400);

        // GET /api/v1/platforms/sync-history — recent sync history
        group.MapGet("/sync-history", async (
            Guid tenantId,
            string? platformFilter,
            int? count,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetSyncHistoryQuery(tenantId, platformFilter, count ?? 20), ct);
            return Results.Ok(result);
        })
        .WithName("GetSyncHistory")
        .WithSummary("Senkronizasyon geçmişi — platform filtreli")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/platforms/sync/trendyol — sync Trendyol products
        group.MapPost("/sync/trendyol", async (
            Guid storeId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SyncTrendyolProductsCommand(storeId), ct);
            return Results.Ok(result);
        })
        .WithName("SyncTrendyolProducts")
        .WithSummary("Trendyol ürün senkronizasyonu başlat")
        .Produces(200).Produces(400);

        // POST /api/v1/platforms/sync/hepsiburada — sync Hepsiburada products
        group.MapPost("/sync/hepsiburada", async (
            Guid storeId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SyncHepsiburadaProductsCommand(storeId), ct);
            return Results.Ok(result);
        })
        .WithName("SyncHepsiburadaProducts")
        .WithSummary("Hepsiburada ürün senkronizasyonu başlat")
        .Produces(200).Produces(400);

        // POST /api/v1/platforms/sync/n11 — sync N11 products
        group.MapPost("/sync/n11", async (
            Guid storeId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SyncN11ProductsCommand(storeId), ct);
            return Results.Ok(result);
        })
        .WithName("SyncN11Products")
        .WithSummary("N11 ürün senkronizasyonu başlat")
        .Produces(200).Produces(400);

        // POST /api/v1/platforms/sync/ciceksepeti — sync Çiçeksepeti products
        group.MapPost("/sync/ciceksepeti", async (
            Guid storeId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SyncCiceksepetiProductsCommand(storeId), ct);
            return Results.Ok(result);
        })
        .WithName("SyncCiceksepetiProducts")
        .WithSummary("Çiçeksepeti ürün senkronizasyonu başlat")
        .Produces(200).Produces(400);

        // POST /api/v1/platforms/map-product — map product to platform category
        group.MapPost("/map-product", async (
            MapProductToPlatformCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(command, ct);
            return Results.NoContent();
        })
        .WithName("MapProductToPlatform")
        .WithSummary("Ürünü platform kategorisine eşle")
        .Produces(204).Produces(400);
    }
}
