using System.Globalization;
using System.Text;
using System.Text.Json;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Formatters.Dropshipping;

/// <summary>
/// HepsiSeller ürün yükleme JSON formatı — productList[] array.
/// ENT-DROP-IMP-SPRINT-B — DEV 3 Görev A (5/6)
/// </summary>
public sealed class HepsisellerDropshippingFormatter : IDropshippingExportFormatter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Platform => "HepsiSeller";

    public Task<byte[]> FormatAsync(
        IEnumerable<PoolProductExportDto> products,
        ExportOptions options,
        CancellationToken ct = default)
    {
        return Task.Run(() => Build(products, options), ct);
    }

    private static byte[] Build(IEnumerable<PoolProductExportDto> products, ExportOptions options)
    {
        var productList = products
            .Where(p => options.IncludeZeroStock || p.Stock > 0)
            .Select(p => BuildProduct(p, options))
            .ToList();

        var payload = new { productList };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        return Encoding.UTF8.GetBytes(json);
    }

    private static object BuildProduct(PoolProductExportDto p, ExportOptions options)
    {
        var markedPrice = ApplyMarkup(p.Price, options.PriceMarkupPercent);

        return new
        {
            merchantSku = p.Sku,
            productName = p.Name,
            productCode = p.Sku,
            barcode = p.Barcode ?? p.Sku,
            categoryName = p.Category ?? "Genel",
            brand = p.Brand ?? "Marka Yok",
            description = p.Description ?? p.Name,
            listingPrice = markedPrice.ToString("F2", CultureInfo.InvariantCulture),
            salePrice = markedPrice.ToString("F2", CultureInfo.InvariantCulture),
            currencyCode = options.Currency,
            stockCount = p.Stock,
            imageUrls = BuildImageList(p.ImageUrl),
            shippingTemplate = "Standart Kargo",
            attributes = new Dictionary<string, string>()
        };
    }

    private static List<string> BuildImageList(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return new List<string>();

        return new List<string> { imageUrl };
    }

    private static decimal ApplyMarkup(decimal price, decimal markupPercent)
    {
        if (markupPercent == 0m) return price;
        return price * (1m + markupPercent / 100m);
    }
}
