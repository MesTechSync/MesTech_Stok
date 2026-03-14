using System.Globalization;
using System.Text;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// UTF-8 BOM CSV export service — Excel uyumlu, Türkçe başlık, quoted fields.
/// ENT-DROP-SENTEZ-001 Sprint A — DEV 3
/// </summary>
public class CsvExportService : ICsvExportService
{
    // UTF-8 BOM — Excel'in Türkçe karakterleri doğru okuyabilmesi için gerekli
    private static readonly Encoding Utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    public async Task<Stream> ExportProductsAsync(IEnumerable<ProductExportDto> products, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(products);
        return await Task.Run(() => BuildProductsCsv(products), ct);
    }

    public async Task<Stream> ExportStockAsync(IEnumerable<StockExportDto> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        return await Task.Run(() => BuildStockCsv(items), ct);
    }

    public async Task<Stream> ExportPricesAsync(IEnumerable<PriceExportDto> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        return await Task.Run(() => BuildPricesCsv(items), ct);
    }

    private static MemoryStream BuildProductsCsv(IEnumerable<ProductExportDto> products)
    {
        var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Utf8Bom, leaveOpen: true);

        // Türkçe başlık satırı
        writer.WriteLine("SKU,Ürün Adı,Fiyat,Stok,Kategori,Barkod");

        foreach (var p in products)
        {
            writer.WriteLine(string.Join(",",
                QuoteField(p.Sku),
                QuoteField(p.Name),
                p.Price.ToString("F2", CultureInfo.InvariantCulture),
                p.Stock.ToString(CultureInfo.InvariantCulture),
                QuoteField(p.Category ?? string.Empty),
                QuoteField(p.Barcode ?? string.Empty)));
        }

        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream BuildStockCsv(IEnumerable<StockExportDto> items)
    {
        var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Utf8Bom, leaveOpen: true);

        writer.WriteLine("SKU,Ürün Adı,Stok");

        foreach (var item in items)
        {
            writer.WriteLine(string.Join(",",
                QuoteField(item.Sku),
                QuoteField(item.Name),
                item.Stock.ToString(CultureInfo.InvariantCulture)));
        }

        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream BuildPricesCsv(IEnumerable<PriceExportDto> items)
    {
        var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Utf8Bom, leaveOpen: true);

        writer.WriteLine("SKU,Ürün Adı,Fiyat");

        foreach (var item in items)
        {
            writer.WriteLine(string.Join(",",
                QuoteField(item.Sku),
                QuoteField(item.Name),
                item.Price.ToString("F2", CultureInfo.InvariantCulture)));
        }

        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// RFC 4180 uyumlu field quoting: virgül, çift tırnak veya satır sonu varsa tırnak içine al.
    /// İçindeki çift tırnak karakterleri çift tırnak ile escape edilir.
    /// </summary>
    private static string QuoteField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
