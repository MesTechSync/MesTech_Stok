using System.Globalization;
using System.Xml;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Infrastructure.Integration.FeedParsers;

/// <summary>
/// Streaming XML feed parser. Uses XmlReader for forward-only, memory-efficient parsing.
/// Supports custom field mapping via FeedFieldMapping.
/// </summary>
public sealed class XmlFeedParser : IFeedParserService
{
    public FeedFormat SupportedFormat => FeedFormat.Xml;

    public Task<FeedParseResult> ParseAsync(Stream feedStream, FeedFieldMapping mapping, CancellationToken ct = default)
    {
        var products = new List<ParsedProduct>();
        var errors = new List<string>();
        var skipped = 0;

        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true,
            DtdProcessing = DtdProcessing.Ignore
        };

        try
        {
            using var reader = XmlReader.Create(feedStream, settings);

            while (reader.Read())
            {
                ct.ThrowIfCancellationRequested();

                if (reader.NodeType != XmlNodeType.Element || !reader.LocalName.Equals("product", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var fields = ReadProductElement(reader);
                    var product = MapToProduct(fields, mapping);

                    if (string.IsNullOrWhiteSpace(product.SKU) && string.IsNullOrWhiteSpace(product.Barcode))
                    {
                        skipped++;
                        errors.Add($"Row {products.Count + skipped}: SKU and Barcode both missing — skipped.");
                        continue;
                    }

                    products.Add(product);
                }
                catch (XmlException ex)
                {
                    skipped++;
                    errors.Add($"Row {products.Count + skipped}: XML parse error — {ex.Message}");
                }
            }
        }
        catch (XmlException ex)
        {
            errors.Add($"Feed XML structure error: {ex.Message}");
        }

        return Task.FromResult(new FeedParseResult(products, products.Count + skipped, skipped, errors));
    }

    public Task<FeedValidationResult> ValidateAsync(Stream feedStream, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var productCount = 0;

        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Ignore
        };

        try
        {
            using var reader = XmlReader.Create(feedStream, settings);
            while (reader.Read())
            {
                ct.ThrowIfCancellationRequested();
                if (reader.NodeType == XmlNodeType.Element &&
                    reader.LocalName.Equals("product", StringComparison.OrdinalIgnoreCase))
                    productCount++;
            }
        }
        catch (XmlException ex)
        {
            errors.Add($"Invalid XML: {ex.Message}");
        }

        return Task.FromResult(new FeedValidationResult(
            errors.Count == 0,
            "XML",
            productCount,
            errors));
    }

    private static Dictionary<string, string> ReadProductElement(XmlReader reader)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (reader.IsEmptyElement)
            return fields;

        // ReadSubtree scopes a sub-reader to this <product> element.
        // ReadElementContentAsString advances the reader past the end tag,
        // landing on the next sibling Element. The inner while loop drains
        // consecutive elements so the outer Read() doesn't skip any.
        using var subtree = reader.ReadSubtree();
        subtree.Read(); // Position on <product>
        while (subtree.Read())
        {
            while (subtree.NodeType == XmlNodeType.Element)
            {
                var name = subtree.LocalName;
                var value = subtree.ReadElementContentAsString();
                fields[name] = value;
            }
        }

        return fields;
    }

    private static ParsedProduct MapToProduct(Dictionary<string, string> fields, FeedFieldMapping mapping)
    {
        var extra = new Dictionary<string, string>();
        var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? Get(string? customField, params string[] defaults)
        {
            if (!string.IsNullOrWhiteSpace(customField) && fields.TryGetValue(customField, out var v))
            {
                mapped.Add(customField);
                return v;
            }
            foreach (var d in defaults)
            {
                if (fields.TryGetValue(d, out var val))
                {
                    mapped.Add(d);
                    return val;
                }
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

        foreach (var kvp in fields)
        {
            if (!mapped.Contains(kvp.Key))
                extra[kvp.Key] = kvp.Value;
        }

        return new ParsedProduct(
            sku, barcode, name, desc,
            decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : null,
            int.TryParse(qtyStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var q) ? q : null,
            category, image, brand, model, extra);
    }
}
