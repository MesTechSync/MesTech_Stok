using System.Diagnostics;
using ClosedXML.Excel;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// ClosedXML tabanlı toplu ürün import/export servisi.
/// Batch processing: 100 satır per SaveChanges.
/// Performance hedefi: 10K satır &lt; 30 saniye.
/// </summary>
public sealed class BulkProductImportService : IBulkProductImportService
{
    private const int BatchSize = 100;

    private static readonly string[] RequiredHeaders =
        ["SKU", "Name", "PurchasePrice", "SalePrice", "Stock"];

    private static readonly string[] AllHeaders =
    [
        "SKU", "Name", "Barcode", "Description",
        "PurchasePrice", "SalePrice", "ListPrice",
        "Stock", "MinimumStock", "MaximumStock",
        "Brand", "Model", "Color", "Size",
        "Weight", "ImageUrl", "Notes", "Tags"
    ];

    private readonly AppDbContext _dbContext;
    private readonly IProductRepository _productRepository;
    private readonly IImportProgressReporter? _progressReporter;

    public BulkProductImportService(
        AppDbContext dbContext,
        IProductRepository productRepository,
        IImportProgressReporter? progressReporter = null)
    {
        _dbContext = dbContext;
        _productRepository = productRepository;
        _progressReporter = progressReporter;
    }

    public Task<ImportValidationResult> ValidateExcelAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ImportRowError>();

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch (Exception ex)
        {
            errors.Add(new ImportRowError(0, "File", $"Geçersiz Excel dosyası: {ex.Message}"));
            return Task.FromResult(new ImportValidationResult(false, 0, 0, 0, errors));
        }

        using (workbook)
        {
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws is null)
            {
                errors.Add(new ImportRowError(0, "File", "Excel dosyasında sayfa bulunamadı."));
                return Task.FromResult(new ImportValidationResult(false, 0, 0, 0, errors));
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2)
            {
                errors.Add(new ImportRowError(0, "File", "Veri satırı bulunamadı."));
                return Task.FromResult(new ImportValidationResult(false, 0, 0, 0, errors));
            }

            // Build header map
            var headerMap = BuildHeaderMap(ws);

            // Validate required headers
            foreach (var required in RequiredHeaders)
            {
                if (!headerMap.ContainsKey(required.ToUpperInvariant()))
                {
                    errors.Add(new ImportRowError(1, required, $"Zorunlu kolon eksik: {required}"));
                }
            }

            if (errors.Count > 0)
            {
                return Task.FromResult(new ImportValidationResult(false, lastRow - 1, 0, lastRow - 1, errors));
            }

            var totalRows = lastRow - 1;
            var validRows = 0;

            for (var row = 2; row <= lastRow; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rowErrors = ValidateRow(ws, row, headerMap);
                if (rowErrors.Count > 0)
                {
                    errors.AddRange(rowErrors);
                }
                else
                {
                    validRows++;
                }
            }

            return Task.FromResult(new ImportValidationResult(
                errors.Count == 0,
                totalRows,
                validRows,
                totalRows - validRows,
                errors));
        }
    }

    public async Task<ImportResult> ImportProductsAsync(
        Stream fileStream,
        ImportOptions options,
        CancellationToken cancellationToken = default)
        => await ImportProductsAsync(fileStream, options, Guid.NewGuid(), cancellationToken).ConfigureAwait(false);

    public async Task<ImportResult> ImportProductsAsync(
        Stream fileStream,
        ImportOptions options,
        Guid importId,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var errors = new List<ImportRowError>();
        var importedCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch (Exception ex)
        {
            errors.Add(new ImportRowError(0, "File", $"Geçersiz Excel dosyası: {ex.Message}"));
            sw.Stop();
            return new ImportResult(ImportStatus.Failed, 0, 0, 0, 0, 1, errors, sw.Elapsed);
        }

        using (workbook)
        {
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws is null)
            {
                errors.Add(new ImportRowError(0, "File", "Excel dosyasında sayfa bulunamadı."));
                sw.Stop();
                return new ImportResult(ImportStatus.Failed, 0, 0, 0, 0, 1, errors, sw.Elapsed);
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            var headerMap = BuildHeaderMap(ws);
            var totalRows = lastRow - 1;
            var batchCount = 0;

            // Pre-load existing SKUs for fast lookup
            var existingSkus = await _dbContext.Set<Product>()
                .AsNoTracking()
                .Select(p => new { p.SKU, p.Id })
                .ToDictionaryAsync(p => p.SKU, p => p.Id, cancellationToken).ConfigureAwait(false);

            for (var row = 2; row <= lastRow; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var rowErrors = ValidateRow(ws, row, headerMap);
                    if (rowErrors.Count > 0)
                    {
                        if (options.SkipErrors)
                        {
                            errors.AddRange(rowErrors);
                            skippedCount++;
                            continue;
                        }

                        errors.AddRange(rowErrors);
                        sw.Stop();
                        return new ImportResult(
                            ImportStatus.Failed, totalRows, importedCount, updatedCount,
                            skippedCount, errors.Count, errors, sw.Elapsed);
                    }

                    var sku = GetCellValue(ws, row, headerMap, "SKU");

                    if (existingSkus.TryGetValue(sku, out var existingId))
                    {
                        if (options.UpdateExisting)
                        {
                            var product = await _dbContext.Set<Product>()
                                .FirstAsync(p => p.Id == existingId, cancellationToken).ConfigureAwait(false);
                            MapRowToProduct(ws, row, headerMap, product, options);
                            product.UpdatedAt = DateTime.UtcNow;
                            updatedCount++;
                        }
                        else
                        {
                            skippedCount++;
                            continue;
                        }
                    }
                    else
                    {
                        var product = new Product();
                        MapRowToProduct(ws, row, headerMap, product, options);
                        product.CreatedAt = DateTime.UtcNow;
                        product.UpdatedAt = DateTime.UtcNow;
                        await _dbContext.Set<Product>().AddAsync(product, cancellationToken).ConfigureAwait(false);
                        existingSkus[product.SKU] = product.Id;
                        importedCount++;
                    }

                    batchCount++;
                    if (batchCount >= BatchSize)
                    {
                        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        batchCount = 0;

                        // Report progress every batch (100 rows)
                        var processed = importedCount + updatedCount + skippedCount;
                        if (_progressReporter != null)
                        {
                            await _progressReporter.ReportProgressAsync(
                                importId, processed, totalRows, errors.Count, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new ImportRowError(row, "Row", $"Beklenmeyen hata: {ex.Message}"));
                    if (!options.SkipErrors)
                    {
                        sw.Stop();
                        return new ImportResult(
                            ImportStatus.Failed, totalRows, importedCount, updatedCount,
                            skippedCount, errors.Count, errors, sw.Elapsed);
                    }

                    skippedCount++;
                }
            }

            // Final batch
            if (batchCount > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            sw.Stop();

            // Report completion
            if (_progressReporter != null)
            {
                await _progressReporter.ReportCompletedAsync(
                    importId, totalRows, importedCount, errors.Count, sw.Elapsed, cancellationToken).ConfigureAwait(false);
            }

            var status = errors.Count > 0 ? ImportStatus.CompletedWithErrors : ImportStatus.Completed;
            return new ImportResult(
                status, totalRows, importedCount, updatedCount,
                skippedCount, errors.Count, errors, sw.Elapsed);
        }
    }

    public async Task<byte[]> ExportProductsAsync(
        BulkExportOptions options,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Product>().AsNoTracking().AsQueryable();

        if (options.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == options.CategoryId.Value);
        }

        if (options.InStock == true)
        {
            query = query.Where(p => p.Stock > 0);
        }
        else if (options.InStock == false)
        {
            query = query.Where(p => p.Stock <= 0);
        }

        var products = await query
            .OrderBy(p => p.SKU)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Ürünler");

        // Header row
        for (var col = 0; col < AllHeaders.Length; col++)
        {
            ws.Cell(1, col + 1).Value = AllHeaders[col];
            ws.Cell(1, col + 1).Style.Font.Bold = true;
            ws.Cell(1, col + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data rows
        for (var i = 0; i < products.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var p = products[i];
            var row = i + 2;

            ws.Cell(row, ColIndex("SKU")).Value = p.SKU;
            ws.Cell(row, ColIndex("Name")).Value = p.Name;
            ws.Cell(row, ColIndex("Barcode")).Value = p.Barcode ?? string.Empty;
            ws.Cell(row, ColIndex("Description")).Value = p.Description ?? string.Empty;
            ws.Cell(row, ColIndex("PurchasePrice")).Value = p.PurchasePrice;
            ws.Cell(row, ColIndex("SalePrice")).Value = p.SalePrice;
            ws.Cell(row, ColIndex("ListPrice")).Value = p.ListPrice ?? 0m;
            ws.Cell(row, ColIndex("Stock")).Value = p.Stock;
            ws.Cell(row, ColIndex("MinimumStock")).Value = p.MinimumStock;
            ws.Cell(row, ColIndex("MaximumStock")).Value = p.MaximumStock;
            ws.Cell(row, ColIndex("Brand")).Value = p.Brand ?? string.Empty;
            ws.Cell(row, ColIndex("Model")).Value = p.Model ?? string.Empty;
            ws.Cell(row, ColIndex("Color")).Value = p.Color ?? string.Empty;
            ws.Cell(row, ColIndex("Size")).Value = p.Size ?? string.Empty;
            ws.Cell(row, ColIndex("Weight")).Value = p.Weight ?? 0m;
            ws.Cell(row, ColIndex("ImageUrl")).Value = p.ImageUrl ?? string.Empty;
            ws.Cell(row, ColIndex("Notes")).Value = p.Notes ?? string.Empty;
            ws.Cell(row, ColIndex("Tags")).Value = p.Tags ?? string.Empty;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Private Helpers ──

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet ws)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (var col = 1; col <= lastCol; col++)
        {
            var header = ws.Cell(1, col).GetString().Trim();
            if (!string.IsNullOrEmpty(header))
            {
                map[header.ToUpperInvariant()] = col;
            }
        }

        return map;
    }

    private static List<ImportRowError> ValidateRow(
        IXLWorksheet ws, int row, Dictionary<string, int> headerMap)
    {
        var errors = new List<ImportRowError>();

        var sku = GetCellValue(ws, row, headerMap, "SKU");
        if (string.IsNullOrWhiteSpace(sku))
        {
            errors.Add(new ImportRowError(row, "SKU", "SKU boş olamaz."));
        }

        var name = GetCellValue(ws, row, headerMap, "Name");
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(new ImportRowError(row, "Name", "Ürün adı boş olamaz."));
        }

        if (!TryGetDecimal(ws, row, headerMap, "PurchasePrice", out var purchasePrice) || purchasePrice < 0)
        {
            errors.Add(new ImportRowError(row, "PurchasePrice", "Alış fiyatı geçerli bir sayı olmalı."));
        }

        if (!TryGetDecimal(ws, row, headerMap, "SalePrice", out var salePrice) || salePrice < 0)
        {
            errors.Add(new ImportRowError(row, "SalePrice", "Satış fiyatı geçerli bir sayı olmalı."));
        }

        if (!TryGetInt(ws, row, headerMap, "Stock", out var stock) || stock < 0)
        {
            errors.Add(new ImportRowError(row, "Stock", "Stok miktarı geçerli bir tam sayı olmalı."));
        }

        return errors;
    }

    private static void MapRowToProduct(
        IXLWorksheet ws, int row, Dictionary<string, int> headerMap,
        Product product, ImportOptions options)
    {
        product.SKU = GetCellValue(ws, row, headerMap, "SKU");
        product.Name = GetCellValue(ws, row, headerMap, "Name");
        product.Barcode = GetCellValueOrNull(ws, row, headerMap, "Barcode");
        product.Description = GetCellValueOrNull(ws, row, headerMap, "Description");

        if (TryGetDecimal(ws, row, headerMap, "PurchasePrice", out var pp))
            product.PurchasePrice = pp;
        if (TryGetDecimal(ws, row, headerMap, "SalePrice", out var sp))
            product.SalePrice = sp;
        if (TryGetDecimal(ws, row, headerMap, "ListPrice", out var lp))
            product.ListPrice = lp;

        if (TryGetInt(ws, row, headerMap, "Stock", out var stock))
            product.Stock = stock;
        if (TryGetInt(ws, row, headerMap, "MinimumStock", out var minStock))
            product.MinimumStock = minStock;
        if (TryGetInt(ws, row, headerMap, "MaximumStock", out var maxStock))
            product.MaximumStock = maxStock;

        product.Brand = GetCellValueOrNull(ws, row, headerMap, "Brand");
        product.Model = GetCellValueOrNull(ws, row, headerMap, "Model");
        product.Color = GetCellValueOrNull(ws, row, headerMap, "Color");
        product.Size = GetCellValueOrNull(ws, row, headerMap, "Size");

        if (TryGetDecimal(ws, row, headerMap, "Weight", out var weight))
            product.Weight = weight;

        product.ImageUrl = GetCellValueOrNull(ws, row, headerMap, "ImageUrl");
        product.Notes = GetCellValueOrNull(ws, row, headerMap, "Notes");
        product.Tags = GetCellValueOrNull(ws, row, headerMap, "Tags");

        if (options.CategoryId.HasValue)
            product.CategoryId = options.CategoryId.Value;
    }

    private static string GetCellValue(
        IXLWorksheet ws, int row, Dictionary<string, int> headerMap, string header)
    {
        if (!headerMap.TryGetValue(header.ToUpperInvariant(), out var col))
            return string.Empty;
        return ws.Cell(row, col).GetString().Trim();
    }

    private static string? GetCellValueOrNull(
        IXLWorksheet ws, int row, Dictionary<string, int> headerMap, string header)
    {
        var value = GetCellValue(ws, row, headerMap, header);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool TryGetDecimal(
        IXLWorksheet ws, int row, Dictionary<string, int> headerMap,
        string header, out decimal value)
    {
        value = 0;
        if (!headerMap.TryGetValue(header.ToUpperInvariant(), out var col))
            return false;

        var cell = ws.Cell(row, col);
        if (cell.TryGetValue(out double d))
        {
            value = (decimal)d;
            return true;
        }

        return decimal.TryParse(cell.GetString().Trim(), out value);
    }

    private static bool TryGetInt(
        IXLWorksheet ws, int row, Dictionary<string, int> headerMap,
        string header, out int value)
    {
        value = 0;
        if (!headerMap.TryGetValue(header.ToUpperInvariant(), out var col))
            return false;

        var cell = ws.Cell(row, col);
        if (cell.TryGetValue(out double d))
        {
            if (double.IsNaN(d) || double.IsInfinity(d) || d > int.MaxValue || d < int.MinValue)
                return false;
            value = (int)d;
            return true;
        }

        return int.TryParse(cell.GetString().Trim(), out value);
    }

    private static int ColIndex(string header)
    {
        return Array.IndexOf(AllHeaders, header) + 1;
    }
}
