using System.Globalization;
using System.Text;
using System.Xml.Linq;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Formatters.Dropshipping;

/// <summary>
/// N11 XML ürün yükleme formatı — SOAP benzeri XML yapısı.
/// Root: &lt;productSaveRequest&gt;&lt;products&gt;&lt;ProductRequest&gt;...&lt;/ProductRequest&gt;&lt;/products&gt;&lt;/productSaveRequest&gt;
/// ENT-DROP-IMP-SPRINT-B — DEV 3 Görev A (6/6)
/// </summary>
public sealed class N11DropshippingFormatter : IDropshippingExportFormatter
{
    public string Platform => "N11";

    public Task<byte[]> FormatAsync(
        IEnumerable<PoolProductExportDto> products,
        ExportOptions options,
        CancellationToken ct = default)
    {
        return Task.Run(() => Build(products, options), ct);
    }

    private static byte[] Build(IEnumerable<PoolProductExportDto> products, ExportOptions options)
    {
        var productElements = products
            .Where(p => options.IncludeZeroStock || p.Stock > 0)
            .Select(p => BuildProductRequest(p, options));

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("productSaveRequest",
                new XElement("products",
                    productElements)));

        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    private static XElement BuildProductRequest(PoolProductExportDto p, ExportOptions options)
    {
        var markedPrice = ApplyMarkup(p.Price, options.PriceMarkupPercent);

        var el = new XElement("ProductRequest",
            new XElement("productSellerCode", p.Sku),
            new XElement("title", p.Name),
            new XElement("subtitle", p.Name),
            new XElement("description", p.Description ?? p.Name),
            new XElement("category",
                new XElement("id", "0"),           // mock — gerçek entegrasyonda kategori mapping
                new XElement("name", p.Category ?? "Genel")),
            new XElement("price", markedPrice.ToString("F2", CultureInfo.InvariantCulture)),
            new XElement("currencyType", MapCurrency(options.Currency)),
            new XElement("images",
                BuildImageElement(p.ImageUrl)),
            new XElement("approvalStatus", "1"),
            new XElement("stockItems",
                new XElement("StockItem",
                    new XElement("quantity", p.Stock.ToString(CultureInfo.InvariantCulture)),
                    new XElement("sellerStockCode", p.Sku),
                    new XElement("optionPrice", markedPrice.ToString("F2", CultureInfo.InvariantCulture)))));

        if (!string.IsNullOrEmpty(p.Barcode))
            el.Add(new XElement("productionDate", string.Empty)); // opsiyonel alanlar

        if (!string.IsNullOrEmpty(p.Brand))
            el.Add(new XElement("specialProductInfos",
                new XElement("SpecialProductInfo",
                    new XElement("name", "Marka"),
                    new XElement("value", p.Brand))));

        return el;
    }

    private static XElement BuildImageElement(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return new XElement("Image",
                new XElement("url", string.Empty),
                new XElement("order", "1"));

        return new XElement("Image",
            new XElement("url", imageUrl),
            new XElement("order", "1"));
    }

    /// <summary>N11 currency type mapping — 1=TL, 2=USD, 3=EUR.</summary>
    private static string MapCurrency(string currency)
    {
        return currency.ToUpperInvariant() switch
        {
            "TRY" or "TL" => "1",
            "USD" => "2",
            "EUR" => "3",
            _ => "1"
        };
    }

    private static decimal ApplyMarkup(decimal price, decimal markupPercent)
    {
        if (markupPercent == 0m) return price;
        return price * (1m + markupPercent / 100m);
    }
}
