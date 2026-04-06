using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Commands.CreateBarcodeScanLog;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.GetBarcodeScanLogs;
using MesTech.Application.Queries.GetProductByBarcode;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class BarcodeEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/barcodes")
            .WithTags("Barcodes")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/barcodes/search?code={barcode} — barkod ile ürün ara
        group.MapGet("/search", async (
            string code,
            ISender mediator, CancellationToken ct) =>
        {
            var safeCode = code is { Length: > 500 } ? code[..500] : code;
            var result = await mediator.Send(
                new GetProductByBarcodeQuery(safeCode), ct);
            return result is not null
                ? Results.Ok(result)
                : Results.Problem(detail: $"Barkod '{code}' ile eşleşen ürün bulunamadı.", statusCode: 404);
        })
        .CacheOutput("Lookup60s")
        .WithName("SearchByBarcode")
        .WithSummary("Barkod ile ürün ara").Produces(200).Produces(400);

        // POST /api/v1/barcodes/scan-log — barkod tarama kaydı oluştur
        group.MapPost("/scan-log", async (
            CreateBarcodeScanLogCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/barcodes/scan-log/{result.LogId}", new CreatedResponse(result.LogId))
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("CreateBarcodeScanLog")
        .WithSummary("Barkod tarama olayı kaydet").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/barcodes/scan-logs — barkod tarama geçmişi
        group.MapGet("/scan-logs", async (
            int page, int pageSize, string? barcode, string? source, bool? isValid,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetBarcodeScanLogsQuery(
                    Math.Max(1, page), Math.Clamp(pageSize, 1, 100), barcode, source, isValid), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetBarcodeScanLogs")
        .WithSummary("Barkod tarama geçmişi (sayfalanmış)").Produces(200).Produces(400);

        // POST /api/v1/barcodes/generate — barkod PNG oluştur (GAP-5 FIX: service mevcut)
        group.MapPost("/generate", (
            BarcodeGenerateRequest request,
            IBarcodeGenerationService barcodeService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Data))
                return Results.Problem(detail: "Barkod verisi boş olamaz.", statusCode: 400);

            var bytes = request.Format?.ToLowerInvariant() switch
            {
                "ean13" => barcodeService.GenerateEan13Png(request.Data, request.Width ?? 300, request.Height ?? 100),
                _ => barcodeService.GenerateCode128Png(request.Data, request.Width ?? 400, request.Height ?? 80)
            };

            var fileName = $"barcode-{request.Data}.png";
            return Results.File(bytes, "image/png", fileName);
        })
        .WithName("GenerateBarcode")
        .WithSummary("Barkod PNG oluştur (Code128 veya EAN-13)")
        .Produces(200).Produces(400);
    }

    private sealed record BarcodeGenerateRequest(string Data, string? Format, int? Width, int? Height);
}
