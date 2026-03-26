using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Services;

public sealed class XmlImportService : IXmlImportService
{
    public async Task<XmlImportResult> ImportProductsAsync(Stream xmlStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(xmlStream);
        return await Task.Run(() => ParseXml(xmlStream, ImportMode.Products), ct);
    }

    public async Task<XmlImportResult> ImportStockAsync(Stream xmlStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(xmlStream);
        return await Task.Run(() => ParseXml(xmlStream, ImportMode.Stock), ct);
    }

    public async Task<XmlImportResult> ImportPricesAsync(Stream xmlStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(xmlStream);
        return await Task.Run(() => ParseXml(xmlStream, ImportMode.Prices), ct);
    }

    private enum ImportMode { Products, Stock, Prices }

    private static XmlImportResult ParseXml(Stream stream, ImportMode mode)
    {
        var readerSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var reader = XmlReader.Create(stream, readerSettings);
        var doc = XDocument.Load(reader);
        var products = doc.Root?.Elements("Product") ?? Enumerable.Empty<XElement>();

        var errors = new List<XmlImportError>();
        var seenSkus = new HashSet<string>(StringComparer.Ordinal);
        int row = 0;
        int successCount = 0;

        foreach (var element in products)
        {
            row++;
            bool rowValid = true;

            // Required: SKU
            var sku = element.Element("SKU")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(sku))
            {
                errors.Add(new XmlImportError { Row = row, Field = "SKU", Message = "SKU is required." });
                rowValid = false;
            }
            else if (!seenSkus.Add(sku))
            {
                errors.Add(new XmlImportError { Row = row, Field = "SKU", Message = $"Duplicate SKU '{sku}' in import batch." });
                rowValid = false;
            }

            // Required: Name
            var name = element.Element("Name")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add(new XmlImportError { Row = row, Field = "Name", Message = "Name is required." });
                rowValid = false;
            }

            // Mode-specific required fields
            if (mode == ImportMode.Products || mode == ImportMode.Prices)
            {
                var priceRaw = element.Element("Price")?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(priceRaw))
                {
                    errors.Add(new XmlImportError { Row = row, Field = "Price", Message = "Price is required." });
                    rowValid = false;
                }
                else if (!decimal.TryParse(priceRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0m)
                {
                    errors.Add(new XmlImportError { Row = row, Field = "Price", Message = "Price must be a valid decimal greater than 0." });
                    rowValid = false;
                }
            }

            if (mode == ImportMode.Stock)
            {
                var stockRaw = element.Element("Stock")?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(stockRaw))
                {
                    errors.Add(new XmlImportError { Row = row, Field = "Stock", Message = "Stock is required." });
                    rowValid = false;
                }
                else if (!int.TryParse(stockRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock) || stock < 0)
                {
                    errors.Add(new XmlImportError { Row = row, Field = "Stock", Message = "Stock must be a valid non-negative integer." });
                    rowValid = false;
                }
            }

            if (rowValid)
            {
                successCount++;
                // Phase 1: parse + validate only — persistence deferred until bulk repository ops are ready
            }
        }

        return new XmlImportResult
        {
            TotalRows = row,
            SuccessCount = successCount,
            FailedCount = row - successCount,
            Errors = errors
        };
    }
}
