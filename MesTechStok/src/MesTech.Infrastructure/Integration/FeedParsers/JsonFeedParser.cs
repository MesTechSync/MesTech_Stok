using System.Globalization;
using System.Text.Json;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Infrastructure.Integration.FeedParsers;

/// <summary>
/// JSON feed parser. Expects an array of product objects, either at root or under a "products" key.
/// Uses System.Text.Json for memory-efficient deserialization.
/// </summary>
public sealed class JsonFeedParser : IFeedParserService
{
    private static readonly JsonDocumentOptions DocOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public FeedFormat SupportedFormat => FeedFormat.Json;

    public async Task<FeedParseResult> ParseAsync(Stream feedStream, FeedFieldMapping mapping, CancellationToken ct = default)
    {
        var products = new List<ParsedProduct>();
        var errors = new List<string>();
        var skipped = 0;

        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(feedStream, DocOptions, ct).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON: {ex.Message}");
            return new FeedParseResult(products, 0, 0, errors);
        }

        using (doc)
        {
            var array = FindProductArray(doc.RootElement);
            if (array is null)
            {
                errors.Add("No product array found — expected root array or 'products' property.");
                return new FeedParseResult(products, 0, 0, errors);
            }

            var index = 0;
            foreach (var element in array.Value.EnumerateArray())
            {
                ct.ThrowIfCancellationRequested();
                index++;

                if (element.ValueKind != JsonValueKind.Object)
                {
                    skipped++;
                    errors.Add($"Item {index}: Not a JSON object — skipped.");
                    continue;
                }

                try
                {
                    var product = MapToProduct(element, mapping);

                    if (string.IsNullOrWhiteSpace(product.SKU) && string.IsNullOrWhiteSpace(product.Barcode))
                    {
                        skipped++;
                        errors.Add($"Item {index}: SKU and Barcode both missing — skipped.");
                        continue;
                    }

                    products.Add(product);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    skipped++;
                    errors.Add($"Item {index}: Parse error — {ex.Message}");
                }
            }
        }

        return new FeedParseResult(products, products.Count + skipped, skipped, errors);
    }

    public async Task<FeedValidationResult> ValidateAsync(Stream feedStream, CancellationToken ct = default)
    {
        var errors = new List<string>();

        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(feedStream, DocOptions, ct).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON: {ex.Message}");
            return new FeedValidationResult(false, "JSON", 0, errors);
        }

        using (doc)
        {
            var array = FindProductArray(doc.RootElement);
            if (array is null)
            {
                errors.Add("No product array found.");
                return new FeedValidationResult(false, "JSON", 0, errors);
            }

            var count = array.Value.GetArrayLength();
            return new FeedValidationResult(true, "JSON", count, errors);
        }
    }

    private static JsonElement? FindProductArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root;

        if (root.ValueKind == JsonValueKind.Object)
        {
            // Try common array property names
            foreach (var name in new[] { "products", "items", "data", "urunler" })
            {
                if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Array)
                    return prop;
            }

            // Case-insensitive fallback
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                    return prop.Value;
            }
        }

        return null;
    }

    private static ParsedProduct MapToProduct(JsonElement element, FeedFieldMapping mapping)
    {
        var extra = new Dictionary<string, string>();
        var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? Get(string? customField, params string[] defaults)
        {
            if (!string.IsNullOrWhiteSpace(customField) &&
                element.TryGetProperty(customField, out var v) && v.ValueKind != JsonValueKind.Null)
            {
                mapped.Add(customField);
                return v.ToString();
            }
            foreach (var d in defaults)
            {
                if (element.TryGetProperty(d, out var val) && val.ValueKind != JsonValueKind.Null)
                {
                    mapped.Add(d);
                    return val.ToString();
                }
            }
            return null;
        }

        var sku = Get(mapping.SkuField, "sku", "stockCode", "stock_code", "productCode");
        var barcode = Get(mapping.BarcodeField, "barcode", "ean", "gtin", "upc");
        var name = Get(mapping.NameField, "name", "title", "productName", "product_name");
        var desc = Get(mapping.DescriptionField, "description", "desc", "details");
        var priceStr = Get(mapping.PriceField, "price", "listPrice", "salePrice");
        var qtyStr = Get(mapping.QuantityField, "quantity", "stock", "qty", "stok");
        var category = Get(mapping.CategoryField, "category", "categoryName", "kategori");
        var image = Get(mapping.ImageField, "image", "imageUrl", "img", "picture");
        var brand = Get(null, "brand", "marka");
        var model = Get(null, "model");

        // Collect unmapped properties as extra fields
        foreach (var prop in element.EnumerateObject())
        {
            if (!mapped.Contains(prop.Name) && prop.Value.ValueKind != JsonValueKind.Null)
                extra[prop.Name] = prop.Value.ToString();
        }

        return new ParsedProduct(
            sku, barcode, name, desc,
            decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : null,
            int.TryParse(qtyStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var q) ? q : null,
            category, image, brand, model, extra);
    }
}
