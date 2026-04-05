using System.Globalization;
using ClosedXML.Excel;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Infrastructure.Integration.FeedParsers;

/// <summary>
/// Excel feed parser using ClosedXML. Reads first worksheet, expects header row in row 1.
/// Supports custom field mapping via FeedFieldMapping.
/// </summary>
public sealed class ExcelFeedParser : IFeedParserService
{
    public FeedFormat SupportedFormat => FeedFormat.Excel;

    public Task<FeedParseResult> ParseAsync(Stream feedStream, FeedFieldMapping mapping, CancellationToken ct = default)
    {
        var products = new List<ParsedProduct>();
        var errors = new List<string>();
        var skipped = 0;

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(feedStream);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            errors.Add($"Invalid Excel file: {ex.Message}");
            return Task.FromResult(new FeedParseResult(products, 0, 0, errors));
        }

        using (workbook)
        {
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws is null)
            {
                errors.Add("No worksheets found in Excel file.");
                return Task.FromResult(new FeedParseResult(products, 0, 0, errors));
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

            if (lastRow < 2 || lastCol < 1)
            {
                return Task.FromResult(new FeedParseResult(products, 0, 0, ["Empty worksheet — no data rows."]));
            }

            // Build header index from row 1
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var col = 1; col <= lastCol; col++)
            {
                var headerVal = ws.Cell(1, col).GetString().Trim();
                if (!string.IsNullOrEmpty(headerVal))
                    headerIndex[headerVal] = col;
            }

            // Parse data rows (row 2 onwards)
            for (var row = 2; row <= lastRow; row++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var product = MapToProduct(ws, row, headerIndex, lastCol, mapping);

                    if (string.IsNullOrWhiteSpace(product.SKU) && string.IsNullOrWhiteSpace(product.Barcode))
                    {
                        skipped++;
                        errors.Add($"Row {row}: SKU and Barcode both missing — skipped.");
                        continue;
                    }

                    products.Add(product);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    skipped++;
                    errors.Add($"Row {row}: Parse error — {ex.Message}");
                }
            }
        }

        return Task.FromResult(new FeedParseResult(products, products.Count + skipped, skipped, errors));
    }

    public Task<FeedValidationResult> ValidateAsync(Stream feedStream, CancellationToken ct = default)
    {
        var errors = new List<string>();

        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(feedStream);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            errors.Add($"Invalid Excel file: {ex.Message}");
            return Task.FromResult(new FeedValidationResult(false, "Excel", 0, errors));
        }

        using (workbook)
        {
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws is null)
            {
                errors.Add("No worksheets found.");
                return Task.FromResult(new FeedValidationResult(false, "Excel", 0, errors));
            }

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            var dataRows = Math.Max(0, lastRow - 1); // Exclude header

            return Task.FromResult(new FeedValidationResult(true, "Excel", dataRows, errors));
        }
    }

    private static ParsedProduct MapToProduct(
        IXLWorksheet ws, int row, Dictionary<string, int> headerIndex, int lastCol, FeedFieldMapping mapping)
    {
        var extra = new Dictionary<string, string>();
        var mapped = new HashSet<int>();

        string? Get(string? customField, params string[] defaults)
        {
            var col = ResolveColumn(customField, defaults, headerIndex);
            if (col.HasValue)
            {
                mapped.Add(col.Value);
                var val = ws.Cell(row, col.Value).GetString()?.Trim();
                return string.IsNullOrEmpty(val) ? null : val;
            }
            return null;
        }

        var sku = Get(mapping.SkuField, "sku", "stockcode", "stock_code", "productcode");
        var barcode = Get(mapping.BarcodeField, "barcode", "ean", "gtin", "upc");
        var name = Get(mapping.NameField, "name", "title", "productname", "product_name");
        var desc = Get(mapping.DescriptionField, "description", "desc", "details");
        var priceStr = Get(mapping.PriceField, "price", "listprice", "saleprice");
        var qtyStr = Get(mapping.QuantityField, "quantity", "stock", "qty", "stok");
        var category = Get(mapping.CategoryField, "category", "categoryname", "kategori");
        var image = Get(mapping.ImageField, "image", "imageurl", "img", "picture");
        var brand = Get(null, "brand", "marka");
        var model = Get(null, "model");

        // Collect unmapped columns as extra fields
        foreach (var kvp in headerIndex)
        {
            if (!mapped.Contains(kvp.Value))
            {
                var val = ws.Cell(row, kvp.Value).GetString().Trim();
                if (!string.IsNullOrEmpty(val))
                    extra[kvp.Key] = val;
            }
        }

        return new ParsedProduct(
            sku, barcode, name, desc,
            decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : null,
            int.TryParse(qtyStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var q) ? q : null,
            category, image, brand, model, extra);
    }

    private static int? ResolveColumn(string? customField, string[] defaults, Dictionary<string, int> headerIndex)
    {
        if (!string.IsNullOrWhiteSpace(customField) && headerIndex.TryGetValue(customField, out var col))
            return col;
        foreach (var d in defaults)
            if (headerIndex.TryGetValue(d, out var c))
                return c;
        return null;
    }
}
