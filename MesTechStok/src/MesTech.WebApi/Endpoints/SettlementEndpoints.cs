using MediatR;
using MesTech.Application.Features.Accounting.Commands.ParseAndImportSettlement;
using MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;

namespace MesTech.WebApi.Endpoints;

public static class SettlementEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/settlement")
            .WithTags("Settlement")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/settlement/upload — platform mutabakat dosyası yükle ve parse et
        group.MapPost("/upload", async (
            Guid tenantId,
            string platform,
            string format,
            HttpRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            using var ms = new MemoryStream();
            await request.Body.CopyToAsync(ms, ct);

            var batchId = await mediator.Send(
                new ParseAndImportSettlementCommand(tenantId, platform, ms.ToArray(), format), ct);

            return Results.Created($"/api/v1/settlement/batches/{batchId}", new { batchId });
        })
        .WithName("UploadSettlement")
        .WithSummary("Platform mutabakat dosyası yükle (JSON/CSV/TSV)")
        .WithDescription("14 platform desteklenir: Trendyol, Amazon, Hepsiburada, N11, Ciceksepeti, Pazarama, OpenCart, eBay, Ozon, PttAVM, Shopify, Etsy, WooCommerce, Zalando")
        .Accepts<IFormFile>("application/json", "text/csv", "text/tab-separated-values")
        .Produces(201)
        .Produces(400)
        .Produces(422);

        // GET /api/v1/settlement/batches — mutabakat batch listesi
        group.MapGet("/batches", async (
            Guid tenantId,
            DateTime? from,
            DateTime? to,
            string? platform,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetSettlementBatchesQuery(tenantId, from, to, platform), ct);
            return Results.Ok(result);
        })
        .WithName("GetSettlementBatches")
        .WithSummary("Mutabakat batch listesi (tarih aralığı + platform filtre)")
        .Produces(200)
        .CacheOutput("Report120s");

        // GET /api/v1/settlement/platforms — desteklenen platform listesi
        group.MapGet("/platforms", (
            Application.Interfaces.Accounting.ISettlementParserFactory factory) =>
        {
            return Results.Ok(new MesTech.Application.DTOs.SupportedPlatformsResponse(factory.SupportedPlatforms, factory.SupportedPlatforms.Count));
        })
        .WithName("GetSettlementPlatforms")
        .WithSummary("Desteklenen mutabakat platformları listesi")
        .Produces(200)
        .CacheOutput("Report120s");
    }
}
