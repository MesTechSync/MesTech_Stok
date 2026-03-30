using System.Globalization;
using System.Text;
using System.Text.Json;
using MesTech.Application.Interfaces;
using MesTech.Application.DTOs;
namespace MesTech.Infrastructure.Formatters.Dropshipping;

/// <summary>
/// Trendyol ürün yükleme JSON formatı — items[] array.
/// Zorunlu alanlar: barcode, title, productMainId, categoryId (mock 1), brandId (mock 1).
/// ENT-DROP-IMP-SPRINT-B — DEV 3 Görev A (4/6)
/// </summary>
public sealed class TrendyolDropshippingFormatter : IDropshippingExportFormatter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Platform => "Trendyol";

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
        var markedPrice = ApplyMarkup(p.Price, options.PriceMarkupPercent);

        return new
        {
            barcode = p.Barcode ?? p.Sku,
            title = p.Name,
            productMainId = p.Sku,
            brandId = 1,        // mock — gerçek entegrasyonda brand mapping yapılır
            categoryId = 1,     // mock — gerçek entegrasyonda kategori mapping yapılır
            quantity = p.Stock,
            stockCode = p.Sku,
            dimensionalWeight = 1,
            description = p.Description ?? p.Name,
            currencyType = options.Currency,
            listPrice = markedPrice.ToString("F2", CultureInfo.InvariantCulture),
            salePrice = markedPrice.ToString("F2", CultureInfo.InvariantCulture),
            vatRate = 18,
            cargoCompanyId = 1,
            images = BuildImages(p.ImageUrl),
            attributes = Array.Empty<object>()
        };
    }

    private static object[] BuildImages(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return Array.Empty<object>();

        return new object[] { new { url = imageUrl } };
    }

    private static decimal ApplyMarkup(decimal price, decimal markupPercent)
    {
        if (markupPercent == 0m) return price;
        return price * (1m + markupPercent / 100m);
    }
}
