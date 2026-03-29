using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Commands.BulkUpdatePrice;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Domain.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Toplu ürün işlemleri: validate, import, export, update.
/// </summary>
public static class BulkProductEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/products/bulk")
            .WithTags("BulkProducts")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/products/bulk/validate — CSV/Excel dosya doğrulama
        group.MapPost("/validate", async (
            HttpRequest httpRequest,
            ISender mediator,
            CancellationToken ct) =>
        {
            if (!httpRequest.HasFormContentType)
                return Results.Problem(detail: "multipart/form-data content type required.", statusCode: 400);

            var form = await httpRequest.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");

            if (file is null || file.Length == 0)
                return Results.Problem(detail: "File is required.", statusCode: 400);

            var allowedExtensions = new[] { ".csv", ".xlsx", ".xls" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return Results.Problem(detail: $"Unsupported file type: {extension}. Allowed: csv, xlsx, xls", statusCode: 400);

            // Dosya boyutu kontrolü (max 10 MB)
            if (file.Length > 10 * 1024 * 1024)
                return Results.Problem(detail: "File size exceeds 10 MB limit.", statusCode: 400);

            return Results.Ok(new
            {
                fileName = file.FileName,
                fileSize = file.Length,
                contentType = file.ContentType,
                isValid = true,
                message = "File validated successfully. Ready for import."
            });
        })
        .WithName("ValidateBulkImport")
        .WithSummary("Toplu ürün import dosyasını doğrula (CSV/Excel)")
        .Produces(200).Produces(400)
        .DisableAntiforgery();

        // POST /api/v1/products/bulk/import — CSV/Excel'den toplu ürün aktarımı
        group.MapPost("/import", async (
            HttpRequest httpRequest,
            ISender mediator,
            CancellationToken ct) =>
        {
            if (!httpRequest.HasFormContentType)
                return Results.Problem(detail: "multipart/form-data content type required.", statusCode: 400);

            var form = await httpRequest.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");

            if (file is null || file.Length == 0)
                return Results.Problem(detail: "File is required.", statusCode: 400);

            var allowedExtensions = new[] { ".csv", ".xlsx", ".xls" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return Results.Problem(detail: $"Unsupported file type: {extension}. Allowed: csv, xlsx, xls", statusCode: 400);

            // Dosyayı belleğe oku
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, ct);
            var fileBytes = memoryStream.ToArray();

            // DEV1-DEPENDENCY: ExecuteBulkImportCommand handler ile parser entegrasyonu bekleniyor
            return Results.Ok(new
            {
                fileName = file.FileName,
                fileSize = file.Length,
                bytesRead = fileBytes.Length,
                status = "queued",
                message = "File accepted for import processing."
            });
        })
        .WithName("ExecuteBulkImport")
        .WithSummary("CSV/Excel dosyasından toplu ürün import et")
        .Produces(200).Produces(400)
        .DisableAntiforgery()
        .WithRequestTimeout("LongRunning");

        // POST /api/v1/products/bulk/export — Ürünleri dosya olarak dışa aktar
        group.MapPost("/export", async (
            BulkExportRequest request,
            IProductRepository productRepository,
            CancellationToken ct) =>
        {
            var format = request.Format?.ToLowerInvariant() ?? "csv";

            // Tüm ürünleri getir
            var products = await productRepository.GetAllAsync();

            if (products.Count == 0)
                return Results.NotFound(new { error = "No products found for export." });

            // CSV formatında dışa aktar
            var csvLines = new List<string>
            {
                "SKU,Name,PurchasePrice,SalePrice,Stock,MinimumStock,CategoryId,IsActive"
            };

            foreach (var p in products)
            {
                csvLines.Add($"\"{p.SKU}\",\"{p.Name}\",{p.PurchasePrice},{p.SalePrice},{p.Stock},{p.MinimumStock},{p.CategoryId},{p.IsActive}");
            }

            var csvContent = string.Join(Environment.NewLine, csvLines);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            var filename = $"mestech-products-{DateTime.UtcNow:yyyyMMddHHmm}.{format}";

            return Results.File(bytes, "text/csv", filename);
        })
        .WithName("ExportProducts")
        .WithSummary("Ürünleri CSV dosyası olarak dışa aktar").Produces(200).Produces(400);

        // POST /api/v1/products/bulk/update — Toplu stok/fiyat güncelleme
        group.MapPost("/update", async (
            BulkUpdateRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            if (request.Items is null || request.Items.Count == 0)
                return Results.Problem(detail: "At least one update item is required.", statusCode: 400);

            var stockItems = request.Items
                .Where(i => i.NewStock.HasValue)
                .Select(i => new BulkUpdateStockItem(i.Sku, i.NewStock!.Value))
                .ToList();

            var priceItems = request.Items
                .Where(i => i.NewPrice.HasValue)
                .Select(i => new BulkUpdatePriceItem(i.Sku, i.NewPrice!.Value))
                .ToList();

            int totalSuccess = 0;
            int totalFailed = 0;
            var allFailures = new List<object>();

            // Stok güncellemesi
            if (stockItems.Count > 0)
            {
                var stockResult = await mediator.Send(
                    new BulkUpdateStockCommand(stockItems), ct);
                totalSuccess += stockResult.SuccessCount;
                totalFailed += stockResult.FailedCount;
                allFailures.AddRange(stockResult.Failures.Select(f => new { f.Sku, f.Reason, Type = "Stock" }));
            }

            // Fiyat güncellemesi
            if (priceItems.Count > 0)
            {
                var priceResult = await mediator.Send(
                    new BulkUpdatePriceCommand(priceItems), ct);
                totalSuccess += priceResult.SuccessCount;
                totalFailed += priceResult.FailedCount;
                allFailures.AddRange(priceResult.Failures.Select(f => new { f.Sku, f.Reason, Type = "Price" }));
            }

            return Results.Ok(new
            {
                successCount = totalSuccess,
                failedCount = totalFailed,
                failures = allFailures
            });
        })
        .WithName("BulkUpdateProducts")
        .WithSummary("Toplu ürün stok ve fiyat güncellemesi").Produces(200).Produces(400);
    }

    /// <summary>
    /// Toplu export istek DTO'su.
    /// </summary>
    public record BulkExportRequest(string? Format = "csv");

    /// <summary>
    /// Toplu güncelleme istek DTO'su.
    /// </summary>
    public record BulkUpdateRequest(List<BulkUpdateItemDto> Items);

    /// <summary>
    /// Tek güncelleme satırı.
    /// </summary>
    public record BulkUpdateItemDto(string Sku, int? NewStock = null, decimal? NewPrice = null);
}
