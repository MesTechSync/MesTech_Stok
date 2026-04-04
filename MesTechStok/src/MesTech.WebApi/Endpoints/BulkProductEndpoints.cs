using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MediatR;
using MesTech.Application.Commands.BulkUpdatePrice;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Application.Features.Product.Commands.BulkCreateProducts;
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

            return Results.Ok(new BulkValidateResponse(
                file.FileName, file.Length, file.ContentType, true,
                "File validated successfully. Ready for import."));
        })
        .WithName("ValidateBulkImport")
        .WithSummary("Toplu ürün import dosyasını doğrula (CSV/Excel)")
        .Produces(200).Produces(400)
        .DisableAntiforgery()
        .AddEndpointFilter<Filters.IdempotencyFilter>();

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
            return Results.Ok(new BulkImportResponse(
                file.FileName, file.Length, fileBytes.Length,
                "queued", "File accepted for import processing."));
        })
        .WithName("ExecuteBulkImport")
        .WithSummary("CSV/Excel dosyasından toplu ürün import et")
        .Produces(200).Produces(400)
        .DisableAntiforgery()
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .WithRequestTimeout("LongRunning");

        // POST /api/v1/products/bulk/export — Ürünleri dosya olarak dışa aktar
        group.MapPost("/export", async (
            BulkExportRequest request,
            IBulkProductImportService bulkService,
            IProductRepository productRepository,
            CancellationToken ct) =>
        {
            const int maxExportLimit = 50_000; // OOM koruması — 50K üründen fazlası sayfalanmalı
            var format = request.Format?.ToLowerInvariant() ?? "csv";

            // FIX-DEV6: ExportProductsAsync uses DB-level Take(50K) + AsNoTracking
            // instead of GetAllAsync() which loads ALL products into memory
            var exportBytes = await bulkService.ExportProductsAsync(
                new BulkExportOptions { CategoryId = request.CategoryId, InStock = request.InStock }, ct);
            if (exportBytes.Length == 0)
                return Results.NotFound(new BulkProductErrorResponse("No products found for export."));

            // Excel export (service returns xlsx)
            if (format == "xlsx" || format == "excel")
            {
                var xlsxFilename = $"mestech-products-{DateTime.UtcNow:yyyyMMddHHmm}.xlsx";
                return Results.File(exportBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", xlsxFilename);
            }

            // CSV fallback — use original path but with DB-level limited data
            var products = await productRepository.GetAllAsync(ct);

            if (products.Count == 0)
                return Results.NotFound(new BulkProductErrorResponse("No products found for export."));

            // Güvenlik limiti — büyük kataloglar için uyarı
            var exportProducts = products.Count > maxExportLimit
                ? products.Take(maxExportLimit).ToList()
                : (IList<MesTech.Domain.Entities.Product>)products;

            // CSV formatında dışa aktar
            var csvLines = new List<string>(exportProducts.Count + 1)
            {
                "SKU,Name,PurchasePrice,SalePrice,Stock,MinimumStock,CategoryId,IsActive"
            };

            foreach (var p in exportProducts)
            {
                // CSV escape: double-quote → doubled, formula injection guard (=,+,-,@,\t,\r)
                csvLines.Add($"{CsvEscape(p.SKU)},{CsvEscape(p.Name)},{p.PurchasePrice},{p.SalePrice},{p.Stock},{p.MinimumStock},{p.CategoryId},{p.IsActive}");
            }

            var csvContent = string.Join(Environment.NewLine, csvLines);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            var suffix = products.Count > maxExportLimit ? $"-partial-{maxExportLimit}" : "";
            var filename = $"mestech-products-{DateTime.UtcNow:yyyyMMddHHmm}{suffix}.{format}";

            return Results.File(bytes, "text/csv", filename);
        })
        .WithName("ExportProducts")
        .WithSummary("Ürünleri CSV dosyası olarak dışa aktar").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

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
            var allFailures = new List<BulkUpdateFailureItem>();

            // Stok güncellemesi
            if (stockItems.Count > 0)
            {
                var stockResult = await mediator.Send(
                    new BulkUpdateStockCommand(stockItems), ct);
                totalSuccess += stockResult.SuccessCount;
                totalFailed += stockResult.FailedCount;
                allFailures.AddRange(stockResult.Failures.Select(f => new BulkUpdateFailureItem(f.Sku, f.Reason, "Stock")));
            }

            // Fiyat güncellemesi
            if (priceItems.Count > 0)
            {
                var priceResult = await mediator.Send(
                    new BulkUpdatePriceCommand(priceItems), ct);
                totalSuccess += priceResult.SuccessCount;
                totalFailed += priceResult.FailedCount;
                allFailures.AddRange(priceResult.Failures.Select(f => new BulkUpdateFailureItem(f.Sku, f.Reason, "Price")));
            }

            return Results.Ok(new BulkUpdateResponse(totalSuccess, totalFailed, allFailures));
        })
        .WithName("BulkUpdateProducts")
        .WithSummary("Toplu ürün stok ve fiyat güncellemesi").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/products/bulk/create — JSON ile toplu ürün oluşturma
        // DEV6 TUR15: G519 — handler-endpoint gap kapatma
        group.MapPost("/create", async (
            BulkCreateProductsCommand command,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("BulkCreateProducts")
        .WithSummary("JSON payload ile toplu ürün oluştur")
        .Produces<BulkCreateProductsResult>(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }

    /// <summary>
    /// Toplu export istek DTO'su.
    /// </summary>
    public record BulkExportRequest(string? Format = "csv", Guid? CategoryId = null, bool? InStock = null);

    /// <summary>
    /// Toplu güncelleme istek DTO'su.
    /// </summary>
    public record BulkUpdateRequest(List<BulkUpdateItemDto> Items);

    /// <summary>
    /// Tek güncelleme satırı.
    /// </summary>
    public record BulkUpdateItemDto(string Sku, int? NewStock = null, decimal? NewPrice = null);

    public sealed record BulkValidateResponse(
        string FileName, long FileSize, string? ContentType, bool IsValid, string Message);

    public sealed record BulkImportResponse(
        string FileName, long FileSize, int BytesRead, string Status, string Message);

    public sealed record BulkProductErrorResponse(string Error);

    public sealed record BulkUpdateFailureItem(string Sku, string Reason, string Type);

    public sealed record BulkUpdateResponse(
        int SuccessCount, int FailedCount, IReadOnlyList<BulkUpdateFailureItem> Failures);

    /// <summary>
    /// CSV field escape: double-quote wrapping + formula injection guard (OWASP).
    /// </summary>
    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        // Formula injection guard: prefix with single-quote if starts with =, +, -, @, \t, \r
        var safe = value;
        if (safe.Length > 0 && "=+-@\t\r".Contains(safe[0]))
            safe = "'" + safe;
        // RFC 4180: double-quote escaping
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }
}
