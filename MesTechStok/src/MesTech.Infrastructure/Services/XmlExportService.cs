using System.Globalization;
using System.Xml.Linq;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Services;

public class XmlExportService : IXmlExportService
{
    public async Task<Stream> ExportProductsAsync(IEnumerable<ProductExportDto> products, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(products);
        return await Task.Run(() => BuildProductsXml(products), ct);
    }

    public async Task<Stream> ExportStockAsync(IEnumerable<StockExportDto> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        return await Task.Run(() => BuildStockXml(items), ct);
    }

    public async Task<Stream> ExportPricesAsync(IEnumerable<PriceExportDto> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        return await Task.Run(() => BuildPricesXml(items), ct);
    }

    private static MemoryStream BuildProductsXml(IEnumerable<ProductExportDto> products)
    {
        var root = new XElement("Products",
            products.Select(p =>
            {
                var el = new XElement("Product",
                    new XElement("SKU", p.Sku),
                    new XElement("Name", p.Name),
                    new XElement("Price", p.Price.ToString("F2", CultureInfo.InvariantCulture)),
                    new XElement("Stock", p.Stock.ToString(CultureInfo.InvariantCulture)));

                if (!string.IsNullOrEmpty(p.Category))
                    el.Add(new XElement("Category", p.Category));

                if (!string.IsNullOrEmpty(p.Barcode))
                    el.Add(new XElement("Barcode", p.Barcode));

                return el;
            }));

        return SerializeToStream(new XDocument(root));
    }

    private static MemoryStream BuildStockXml(IEnumerable<StockExportDto> items)
    {
        var root = new XElement("Products",
            items.Select(i => new XElement("Product",
                new XElement("SKU", i.Sku),
                new XElement("Name", i.Name),
                new XElement("Stock", i.Stock.ToString(CultureInfo.InvariantCulture)))));

        return SerializeToStream(new XDocument(root));
    }

    private static MemoryStream BuildPricesXml(IEnumerable<PriceExportDto> items)
    {
        var root = new XElement("Products",
            items.Select(i => new XElement("Product",
                new XElement("SKU", i.Sku),
                new XElement("Name", i.Name),
                new XElement("Price", i.Price.ToString("F2", CultureInfo.InvariantCulture)))));

        return SerializeToStream(new XDocument(root));
    }

    private static MemoryStream SerializeToStream(XDocument doc)
    {
        var ms = new MemoryStream();
        doc.Save(ms);
        ms.Position = 0;
        return ms;
    }
}
