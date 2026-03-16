using System.Globalization;
using System.Reflection;
using System.Text;
using MesTech.Application.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Generic rapor export servisi — reflection ile herhangi bir DTO tipini Excel veya CSV'ye cevirir.
/// EPPlus (NonCommercial lisans) Excel uretimi, UTF-8 BOM CSV export.
/// </summary>
public class ReportExportService : IReportExportService
{
    // UTF-8 BOM — Excel'in Turkce karakterleri dogru okuyabilmesi icin gerekli
    private static readonly Encoding Utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    static ReportExportService()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportToExcelAsync<T>(
        IEnumerable<T> data, string sheetName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);

        return await Task.Run(() => BuildExcel(data, sheetName), ct);
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportToCsvAsync<T>(
        IEnumerable<T> data, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        return await Task.Run(() => BuildCsv(data), ct);
    }

    private static byte[] BuildExcel<T>(IEnumerable<T> data, string sheetName)
    {
        var properties = GetPublicProperties<T>();

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add(sheetName);

        // Header row
        for (var col = 0; col < properties.Length; col++)
        {
            ws.Cells[1, col + 1].Value = properties[col].Name;
            ws.Cells[1, col + 1].Style.Font.Bold = true;
        }

        // Data rows
        var row = 2;
        foreach (var item in data)
        {
            for (var col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(item);
                ws.Cells[row, col + 1].Value = FormatValue(value);
            }
            row++;
        }

        // Auto-fit columns for readability
        if (row > 2)
        {
            ws.Cells[1, 1, row - 1, properties.Length].AutoFitColumns();
        }

        return package.GetAsByteArray();
    }

    private static byte[] BuildCsv<T>(IEnumerable<T> data)
    {
        var properties = GetPublicProperties<T>();

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Utf8Bom, leaveOpen: true);

        // Header row
        writer.WriteLine(string.Join(",", properties.Select(p => QuoteField(p.Name))));

        // Data rows
        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return QuoteField(FormatValue(value));
            });
            writer.WriteLine(string.Join(",", values));
        }

        writer.Flush();
        return ms.ToArray();
    }

    private static PropertyInfo[] GetPublicProperties<T>()
    {
        return typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .OrderBy(p => p.MetadataToken) // Preserve declaration order
            .ToArray();
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            decimal d => d.ToString("F2", CultureInfo.InvariantCulture),
            double d => d.ToString("F2", CultureInfo.InvariantCulture),
            float f => f.ToString("F2", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            Guid g => g.ToString(),
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// RFC 4180 uyumlu field quoting: virgul, cift tirnak veya satir sonu varsa tirnak icine al.
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
