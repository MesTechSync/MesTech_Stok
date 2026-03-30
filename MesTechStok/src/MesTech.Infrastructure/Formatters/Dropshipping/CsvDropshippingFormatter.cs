using System.Globalization;
using System.Text;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Formatters.Dropshipping;

/// <summary>
/// UTF-8 BOM CSV formatter — Excel uyumlu, Türkçe başlık satırı, RFC 4180 quoting.
/// ENT-DROP-IMP-SPRINT-B — DEV 3 Görev A (2/6)
/// </summary>
public sealed class CsvDropshippingFormatter : IDropshippingExportFormatter
{
    // UTF-8 BOM — Excel Türkçe karakter uyumu için gerekli
    private static readonly Encoding Utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    public string Platform => "CSV";

    public Task<byte[]> FormatAsync(
        IEnumerable<PoolProductExportDto> products,
        ExportOptions options,
        CancellationToken ct = default)
    {
        return Task.Run(() => Build(products, options), ct);
    }

    private static byte[] Build(IEnumerable<PoolProductExportDto> products, ExportOptions options)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Utf8Bom, leaveOpen: true);

        // Türkçe başlık satırı
        writer.WriteLine("SKU,Ürün Adı,Barkod,Fiyat,Para Birimi,Stok,Kategori,Marka,Görsel URL,Açıklama");

        foreach (var p in products)
        {
            if (!options.IncludeZeroStock && p.Stock <= 0)
                continue;

            var markedPrice = ApplyMarkup(p.Price, options.PriceMarkupPercent);

            writer.WriteLine(string.Join(",",
                QuoteField(p.Sku),
                QuoteField(p.Name),
                QuoteField(p.Barcode ?? string.Empty),
                markedPrice.ToString("F2", CultureInfo.InvariantCulture),
                QuoteField(options.Currency),
                p.Stock.ToString(CultureInfo.InvariantCulture),
                QuoteField(p.Category ?? string.Empty),
                QuoteField(p.Brand ?? string.Empty),
                QuoteField(p.ImageUrl ?? string.Empty),
                QuoteField(p.Description ?? string.Empty)));
        }

        writer.Flush();
        ms.Position = 0;
        return ms.ToArray();
    }

    /// <summary>
    /// RFC 4180 uyumlu field quoting + CSV injection guard.
    /// G107 FIX: =, +, -, @ prefix'li değerler Excel'de formül olarak çalışır.
    /// Apostrof prefix ile neutralize edilir.
    /// </summary>
    private static string QuoteField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // CSV injection guard — Excel formula prefix neutralization
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = $"'{value}";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }

    private static decimal ApplyMarkup(decimal price, decimal markupPercent)
    {
        if (markupPercent == 0m) return price;
        return price * (1m + markupPercent / 100m);
    }
}
