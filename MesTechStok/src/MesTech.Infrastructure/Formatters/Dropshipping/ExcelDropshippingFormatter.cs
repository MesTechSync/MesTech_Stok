using System.Globalization;
using MesTech.Application.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace MesTech.Infrastructure.Formatters.Dropshipping;

/// <summary>
/// Excel (XLSX) formatter — EPPlus, bold başlık satırı, uygun kolon genişlikleri, sayı formatları.
/// ENT-DROP-IMP-SPRINT-B — DEV 3 Görev A (3/6)
/// </summary>
public sealed class ExcelDropshippingFormatter : IDropshippingExportFormatter
{
    static ExcelDropshippingFormatter()
    {
        ExcelPackage.License.SetNonCommercialPersonal("MesTech");
    }

    public string Platform => "Excel";

    public Task<byte[]> FormatAsync(
        IEnumerable<PoolProductExportDto> products,
        ExportOptions options,
        CancellationToken ct = default)
    {
        return Task.Run(() => Build(products, options), ct);
    }

    private static byte[] Build(IEnumerable<PoolProductExportDto> products, ExportOptions options)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Dropshipping Ürünleri");

        // Başlık satırı
        ws.Cells[1, 1].Value = "SKU";
        ws.Cells[1, 2].Value = "Ürün Adı";
        ws.Cells[1, 3].Value = "Barkod";
        ws.Cells[1, 4].Value = "Fiyat";
        ws.Cells[1, 5].Value = "Para Birimi";
        ws.Cells[1, 6].Value = "Stok";
        ws.Cells[1, 7].Value = "Kategori";
        ws.Cells[1, 8].Value = "Marka";
        ws.Cells[1, 9].Value = "Görsel URL";
        ws.Cells[1, 10].Value = "Açıklama";

        // Bold + arka plan rengi için başlık satırını formatla
        using (var headerRange = ws.Cells[1, 1, 1, 10])
        {
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
            headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        int row = 2;
        foreach (var p in products)
        {
            if (!options.IncludeZeroStock && p.Stock <= 0)
                continue;

            var markedPrice = ApplyMarkup(p.Price, options.PriceMarkupPercent);

            ws.Cells[row, 1].Value = p.Sku;
            ws.Cells[row, 2].Value = p.Name;
            ws.Cells[row, 3].Value = p.Barcode ?? string.Empty;

            // Sayı formatı — fiyat
            ws.Cells[row, 4].Value = markedPrice;
            ws.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";

            ws.Cells[row, 5].Value = options.Currency;

            // Sayı formatı — stok
            ws.Cells[row, 6].Value = p.Stock;
            ws.Cells[row, 6].Style.Numberformat.Format = "#,##0";

            ws.Cells[row, 7].Value = p.Category ?? string.Empty;
            ws.Cells[row, 8].Value = p.Brand ?? string.Empty;
            ws.Cells[row, 9].Value = p.ImageUrl ?? string.Empty;
            ws.Cells[row, 10].Value = p.Description ?? string.Empty;

            row++;
        }

        // Kolon genişlikleri
        ws.Column(1).Width = 18;   // SKU
        ws.Column(2).Width = 40;   // Ürün Adı
        ws.Column(3).Width = 16;   // Barkod
        ws.Column(4).Width = 14;   // Fiyat
        ws.Column(5).Width = 12;   // Para Birimi
        ws.Column(6).Width = 10;   // Stok
        ws.Column(7).Width = 20;   // Kategori
        ws.Column(8).Width = 18;   // Marka
        ws.Column(9).Width = 40;   // Görsel URL
        ws.Column(10).Width = 50;  // Açıklama

        using var ms = new MemoryStream();
        package.SaveAs(ms);
        return ms.ToArray();
    }

    private static decimal ApplyMarkup(decimal price, decimal markupPercent)
    {
        if (markupPercent == 0m) return price;
        return price * (1m + markupPercent / 100m);
    }
}
