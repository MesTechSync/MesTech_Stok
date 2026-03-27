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

            var result = await mediator.Send(
                new GetPoolProductsQuery(poolId, color, search, page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize), ct);
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
