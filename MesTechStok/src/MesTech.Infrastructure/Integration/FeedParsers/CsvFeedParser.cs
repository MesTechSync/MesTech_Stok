using System.Globalization;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Infrastructure.Integration.FeedParsers;

/// <summary>
/// CSV feed parser. Reads header row to detect field names, then streams line-by-line.
/// Supports custom field mapping via FeedFieldMapping.
/// </summary>
public sealed class CsvFeedParser : IFeedParserService
{
    public FeedFormat SupportedFormat => FeedFormat.Csv;

    public async Task<FeedParseResult> ParseAsync(Stream feedStream, FeedFieldMapping mapping, CancellationToken ct = default)
    {
        var products = new List<ParsedProduct>();
        var errors = new List<string>();
        var skipped = 0;

        using var reader = new StreamReader(feedStream, leaveOpen: true);

        var headerLine = await reader.ReadLineAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return new FeedParseResult(products, 0, 0, ["Empty feed — no header row."]);
        }

        var headers = ParseCsvLine(headerLine);
        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
            headerIndex[headers[i].Trim()] = i;

        var lineNumber = 1;
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            lineNumber++;

            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var values = ParseCsvLine(line);
                var product = MapToProduct(headerIndex, values, mapping);

                if (string.IsNullOrWhiteSpace(product.SKU) && string.IsNullOrWhiteSpace(product.Barcode))
                {
                    skipped++;
                    errors.Add($"Line {lineNumber}: SKU and Barcode both missing — skipped.");
                    continue;
                }

                products.Add(product);
            }
            catch (Exception ex)
            {
                skipped++;
                errors.Add($"Line {lineNumber}: Parse error — {ex.Message}");
            }
        }

        return new FeedParseResult(products, products.Count + skipped, skipped, errors);
    }

    public async Task<FeedValidationResult> ValidateAsync(Stream feedStream, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var lineCount = 0;

        using var reader = new StreamReader(feedStream, leaveOpen: true);

        var headerLine = await reader.ReadLineAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            errors.Add("Empty feed — no header row.");
            return new FeedValidationResult(false, "CSV", 0, errors);
        }

        var headers = ParseCsvLine(headerLine);
        if (headers.Length < 2)
            errors.Add("Header row has fewer than 2 columns — possibly wrong delimiter.");

        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(line))
                lineCount++;
        }

        return new FeedValidationResult(errors.Count == 0, "CSV", lineCount, errors);
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }

    private static ParsedProduct MapToProduct(
        Dictionary<string, int> headerIndex, string[] values, FeedFieldMapping mapping)
    {
        var extra = new Dictionary<string, string>();
        var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? Get(string? customField, params string[] defaults)
        {
            var field = ResolveField(customField, defaults);
            if (field != null && headerIndex.TryGetValue(field, out var idx) && idx < values.Length)
            {
                mapped.Add(field);
                var val = values[idx].Trim();
                return string.IsNullOrEmpty(val) ? null : val;
            }
            return null;
        }

        string? ResolveField(string? custom, string[] defaults)
        {
            if (!string.IsNullOrWhiteSpace(custom) && headerIndex.ContainsKey(custom))
                return custom;
            foreach (var d in defaults)
                if (headerIndex.ContainsKey(d))
                    return d;
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
            if (!mapped.Contains(kvp.Key) && kvp.Value < values.Length)
            {
                var val = values[kvp.Value].Trim();
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
}
