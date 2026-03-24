using MediatR;
using MesTech.Application.Commands.CreateBarcodeScanLog;
using MesTech.Application.Queries.GetBarcodeScanLogs;
using MesTech.Application.Queries.GetProductByBarcode;

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
            var result = await mediator.Send(
                new GetProductByBarcodeQuery(code), ct);
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new { Message = $"Barkod '{code}' ile eşleşen ürün bulunamadı" });
        })
        .WithName("SearchByBarcode")
        .WithSummary("Barkod ile ürün ara");

        // POST /api/v1/barcodes/scan-log — barkod tarama kaydı oluştur
        group.MapPost("/scan-log", async (
            CreateBarcodeScanLogCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/barcodes/scan-log/{result.LogId}", new { result.LogId })
                : Results.BadRequest(new { result.ErrorMessage });
        })
        .WithName("CreateBarcodeScanLog")
        .WithSummary("Barkod tarama olayı kaydet");

        // GET /api/v1/barcodes/scan-logs — barkod tarama geçmişi
        group.MapGet("/scan-logs", async (
            int page, int pageSize, string? barcode, string? source, bool? isValid,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetBarcodeScanLogsQuery(page, pageSize, barcode, source, isValid), ct);
            return Results.Ok(result);
        })
        .WithName("GetBarcodeScanLogs")
        .WithSummary("Barkod tarama geçmişi (sayfalanmış)");
    }
}
