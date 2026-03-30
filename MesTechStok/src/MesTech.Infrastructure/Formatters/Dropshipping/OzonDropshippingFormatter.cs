using System.Globalization;
using System.Text;
using System.Text.Json;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Formatters.Dropshipping;

/// <summary>
/// Ozon pazaryeri ürün yükleme JSON formatı — items[] array (Ozon API v3).
/// Ozon Türkiye üzerinden satış yapan TR satıcıları için.
/// Zorunlu: offer_id (SKU), name, price, count.
/// ENT-DROP-IMP-SPRINT-D — DEV 3 Task D-08 (9/9 export format)
/// </summary>
public sealed class OzonDropshippingFormatter : IDropshippingExportFormatter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented          = false,
        PropertyNamingPolicy   = null, // anonymous type property names are already snake_case
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public string Platform => "Ozon";

    public Task<byte[]> FormatAsync(
        IEnumerable<PoolProductExportDto> products,
        ExportOptions options,
        CancellationToken ct = default)
    {
        return Task.Run(() => Build(products, options), ct);
    }

    private static byte[] Build(IEnumerable<PoolProductExportDto> products, ExportOptions options)
    {
        var items = products
            .Where(p => options.IncludeZeroStock || p.Stock > 0)
            .Select(p => BuildItem(p, options))
            .ToList();

        var payload = new { items };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        return Encoding.UTF8.GetBytes(json);
    }

    private static object BuildItem(PoolProductExportDto p, ExportOptions options)
    {
        var price    = ApplyMarkup(p.Price, options.PriceMarkupPercent);
        var oldPrice = price * 1.10m; // göstermelik liste fiyatı (%10 üzeri)

        return new
        {
            name          = p.Name,
            offer_id      = p.Sku,
            barcode       = p.Barcode ?? string.Empty,
            price         = price.ToString("F2", CultureInfo.InvariantCulture),
            old_price     = oldPrice.ToString("F2", CultureInfo.InvariantCulture),
            vat           = "0.2",           // %20 KDV (Ozon TR)
            count         = p.Stock,
            currency_code = options.Currency,
            category_id   = 0,               // mock — gerçek entegrasyonda kategori mapping
            description   = p.Description ?? p.Name,
            images        = BuildImages(p.ImageUrl),
            attributes    = Array.Empty<object>()
        };
    }

    private static string[] BuildImages(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return Array.Empty<string>();

        return new[] { imageUrl };
    }

    private static decimal ApplyMarkup(decimal price, decimal markupPercent)
    {
        if (markupPercent == 0m) return price;
        return price * (1m + markupPercent / 100m);
    }
}
