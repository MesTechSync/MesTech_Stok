using System.Globalization;
using System.Text;
using System.Xml.Linq;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Formatters.Dropshipping;

/// <summary>
/// Generic XML formatter — teknotok/viyamo feed uyumlu.
/// Root: &lt;Products&gt;&lt;Product&gt;...&lt;/Product&gt;&lt;/Products&gt;
/// Encoding: UTF-8 with XML declaration.
/// ENT-DROP-IMP-SPRINT-B — DEV 3 Görev A (1/6)
/// </summary>
public sealed class XmlDropshippingFormatter : IDropshippingExportFormatter
{
    public string Platform => "XML";

    public Task<byte[]> FormatAsync(
        IEnumerable<PoolProductExportDto> products,
        ExportOptions options,
        CancellationToken ct = default)
    {
        return Task.Run(() => Build(products, options), ct);
    }

    private static byte[] Build(IEnumerable<PoolProductExportDto> products, ExportOptions options)
    {
        var productList = products.ToList();

        var productElements = productList
            .Where(p => options.IncludeZeroStock || p.Stock > 0)
            .Select(p => BuildProductElement(p, options));

        var root = new XElement("Products", productElements);
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            root);

        using var ms = new MemoryStream();
        // UTF-8 encoding — XDocument.Save kullanır
        doc.Save(ms);
        return ms.ToArray();
    }

    private static XElement BuildProductElement(PoolProductExportDto p, ExportOptions options)
    {
        var markedPrice = ApplyMarkup(p.Price, options.PriceMarkupPercent);

        var el = new XElement("Product",
            new XElement("SKU", p.Sku),
            new XElement("Name", p.Name),
            new XElement("Price", markedPrice.ToString("F2", CultureInfo.InvariantCulture)),
            new XElement("Currency", options.Currency),
            new XElement("Stock", p.Stock.ToString(CultureInfo.InvariantCulture)));

        if (!string.IsNullOrEmpty(p.Barcode))
            el.Add(new XElement("Barcode", p.Barcode));

        if (!string.IsNullOrEmpty(p.Category))
            el.Add(new XElement("Category", p.Category));

        if (!string.IsNullOrEmpty(p.Brand))
            el.Add(new XElement("Brand", p.Brand));

        if (!string.IsNullOrEmpty(p.ImageUrl))
            el.Add(new XElement("ImageUrl", p.ImageUrl));

        if (!string.IsNullOrEmpty(p.Description))
            el.Add(new XElement("Description", new XCData(p.Description)));

        if (!options.HideSupplierInfo && !string.IsNullOrEmpty(p.SupplierName))
            el.Add(new XElement("SupplierName", p.SupplierName));

        return el;
    }

    private static decimal ApplyMarkup(decimal price, decimal markupPercent)
    {
        if (markupPercent == 0m) return price;
        return price * (1m + markupPercent / 100m);
    }
}
