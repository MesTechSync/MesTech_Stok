using MediatR;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Features.Dropshipping.Queries;
using MesTech.Application.Interfaces;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class DropshippingPoolEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/dropshipping")
            .WithTags("DropshippingPool")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/dropshipping/pool/products — havuz ürün listesi (filtrelenebilir)
        group.MapGet("/pool/products", async (
            ISender mediator,
            Guid? poolId,
            string? colorFilter,
            string? search,
            int page,
            int pageSize,
            CancellationToken ct) =>
        {
            ReliabilityColor? color = null;
            if (!string.IsNullOrEmpty(colorFilter) &&
                Enum.TryParse<ReliabilityColor>(colorFilter, ignoreCase: true, out var parsed))
                color = parsed;

            var safeSearch = search is { Length: > 500 } ? search[..500] : search;
            var result = await mediator.Send(
                new GetPoolProductsQuery(poolId, color, safeSearch, page <= 0 ? 1 : page, Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetPoolProducts")
        .WithSummary("Dropshipping havuz ürünleri (güvenilirlik rengi + arama filtresi)").Produces(200).Produces(400);

        // GET /api/v1/dropshipping/pool/stats — havuz özet istatistikleri
        group.MapGet("/pool/stats", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPoolDashboardStatsQuery(), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetPoolStats")
        .WithSummary("Dropshipping havuzu özet istatistikleri").Produces(200).Produces(400);

        // GET /api/v1/dropshipping/pool/products/{id} — tekil havuz ürünü (G207-DEV6)
        group.MapGet("/pool/products/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPoolProductByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .CacheOutput("Lookup60s")
        .WithName("GetPoolProductById")
        .WithSummary("Tekil dropshipping havuz ürünü detayı").Produces(200).Produces(404);

        // GET /api/v1/dropshipping/pools — havuz listesi (G207-DEV6)
        group.MapGet("/pools", async (
            bool? isActive, int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetDropshippingPoolsQuery(isActive, Math.Max(1, page), Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetDropshippingPools")
        .WithSummary("Dropshipping havuz listesi (aktif/pasif filtreli)").Produces(200);

        // GET /api/v1/dropshipping/pool/reliability/{feedId} — tedarikçi güvenilirlik (G207-DEV6)
        group.MapGet("/pool/reliability/{feedId:guid}", async (
            Guid feedId, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSupplierReliabilityQuery(feedId), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetSupplierReliability")
        .WithSummary("Tedarikçi güvenilirlik skoru (renk + trend)").Produces(200);

        // GET /api/v1/dropshipping/pool/export-history — export geçmişi (G207-DEV6)
        group.MapGet("/pool/export-history", async (
            Guid? poolId, int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetExportHistoryQuery(poolId, Math.Max(1, page), Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetExportHistory")
        .WithSummary("Dropshipping export geçmişi").Produces(200);

        // GET /api/v1/dropshipping/pool/feeds — feed kaynakları listesi (G207-DEV6)
        group.MapGet("/pool/feeds", async (
            bool? isActive, int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetFeedSourcesQuery(isActive, Math.Max(1, page), Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetFeedSources")
        .WithSummary("Feed kaynak listesi (aktif/pasif filtreli)").Produces(200);

        // GET /api/v1/dropshipping/pool/feeds/{feedId} — tekil feed kaynağı (G207-DEV6)
        group.MapGet("/pool/feeds/{feedId:guid}", async (
            Guid feedId, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetFeedSourceByIdQuery(feedId), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .CacheOutput("Lookup60s")
        .WithName("GetFeedSourceById")
        .WithSummary("Tekil feed kaynağı detayı").Produces(200).Produces(404);

        // GET /api/v1/dropshipping/pool/feeds/{feedId}/history — feed import geçmişi (G207-DEV6)
        group.MapGet("/pool/feeds/{feedId:guid}/history", async (
            Guid feedId, int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetFeedImportHistoryQuery(feedId, Math.Max(1, page), Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetFeedImportHistory")
        .WithSummary("Feed import geçmişi (sayfalanmış)").Produces(200);

        // POST /api/v1/dropshipping/pool/export — havuz ürünleri platforma export et
        group.MapPost("/pool/export", async (
            ExportPoolProductsToPlatformCommand command, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("ExportPoolToPlatform")
        .WithSummary("Seçili havuz ürünlerini platforma aktar (Trendyol, Hepsiburada, vb.)").Produces(200).Produces(400);

        // POST /api/v1/dropshipping/pool/export/xml — XML feed olarak dışa aktar
        group.MapPost("/pool/export/xml", async (
            ExportPoolProductsToXmlCommand command, ISender mediator, CancellationToken ct) =>
        {
            var bytes = await mediator.Send(command, ct);
            var filename = $"mestech-dropshipping-{DateTime.UtcNow:yyyyMMddHHmm}.xml";
            return Results.File(bytes, "application/xml", filename);
        })
        .WithName("ExportPoolToXml")
        .WithSummary("Seçili havuz ürünlerini XML feed olarak indir").Produces(200).Produces(400);
    }
}
