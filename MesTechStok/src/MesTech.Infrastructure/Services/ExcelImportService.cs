using System.Globalization;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using OfficeOpenXml;

namespace MesTech.Infrastructure.Services;

public sealed class ExcelImportService : IExcelImportService
{
    static ExcelImportService()
    {
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
    }

    public async Task<XmlImportResult> ImportProductsAsync(Stream excelStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(excelStream);
        return await Task.Run(() => ParseExcel(excelStream, ImportMode.Products), ct);
    }

    public async Task<XmlImportResult> ImportStockAsync(Stream excelStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(excelStream);
        return await Task.Run(() => ParseExcel(excelStream, ImportMode.Stock), ct);
    }

    public async Task<XmlImportResult> ImportPricesAsync(Stream excelStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(excelStream);
        return await Task.Run(() => ParseExcel(excelStream, ImportMode.Prices), ct);
    }

    private enum ImportMode { Products, Stock, Prices }

    private static XmlImportResult ParseExcel(Stream stream, ImportMode mode)
    {
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.Count > 0
            ? package.Workbook.Worksheets[0]
            : null;

        var errors = new List<XmlImportError>();

        if (worksheet == null || worksheet.Dimension == null)
        {
            return new XmlImportResult
            {
                TotalRows = 0,
                SuccessCount = 0,
                FailedCount = 0,
                Errors = errors
            };
        }

        // Build column index map from header row (row 1)
        var headerMap = BuildHeaderMap(worksheet);

        // Validate required columns exist
        int skuCol = FindColumn(headerMap, "sku");
        int nameCol = FindColumn(headerMap, "name", "ad", "ürün adı");
        int priceCol = FindColumn(headerMap, "price", "fiyat");
        int stockCol = FindColumn(headerMap, "stock", "stok");

        if (skuCol < 0)
        {
            errors.Add(new XmlImportError { Row = 0, Field = "SKU", Message = "SKU column not found in header row." });
            return new XmlImportResult { TotalRows = 0, SuccessCount = 0, FailedCount = 0, Errors = errors };
        }

        if (nameCol < 0)
        {
            errors.Add(new XmlImportError { Row = 0, Field = "Name", Message = "Name column not found in header row." });
            return new XmlImportResult { TotalRows = 0, SuccessCount = 0, FailedCount = 0, Errors = errors };
        }

        if ((mode == ImportMode.Products || mode == ImportMode.Prices) && priceCol < 0)
        {
            errors.Add(new XmlImportError { Row = 0, Field = "Price", Message = "Price column not found in header row." });
            return new XmlImportResult { TotalRows = 0, SuccessCount = 0, FailedCount = 0, Errors = errors };
        }

        if (mode == ImportMode.Stock && stockCol < 0)
        {
            errors.Add(new XmlImportError { Row = 0, Field = "Stock", Message = "Stock column not found in header row." });
            return new XmlImportResult { TotalRows = 0, SuccessCount = 0, FailedCount = 0, Errors = errors };
        }

        int totalRows = worksheet.Dimension.End.Row;
        var seenSkus = new HashSet<string>(StringComparer.Ordinal);
        int dataRow = 0;
        int successCount = 0;

        // Data starts from row 2 (row 1 is header)
        for (int row = 2; row <= totalRows; row++)
        {
            // Skip completely empty rows
            bool allEmpty = IsRowEmpty(worksheet, row, worksheet.Dimension.End.Column);
            if (allEmpty)
                continue;

            dataRow++;
            bool rowValid = true;

            // Required: SKU
            var sku = GetCellString(worksheet, row, skuCol);
            if (string.IsNullOrWhiteSpace(sku))
            {
                errors.Add(new XmlImportError { Row = dataRow, Field = "SKU", Message = "SKU is required." });
                rowValid = false;
            }
            else if (!seenSkus.Add(sku))
            {
                errors.Add(new XmlImportError { Row = dataRow, Field = "SKU", Message = $"Duplicate SKU '{sku}' in import batch." });
                rowValid = false;
            }

            // Required: Name
            var name = GetCellString(worksheet, row, nameCol);
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add(new XmlImportError { Row = dataRow, Field = "Name", Message = "Name is required." });
                rowValid = false;
            }

            // Mode-specific required fields
            if (mode == ImportMode.Products || mode == ImportMode.Prices)
            {
                var priceRaw = GetCellString(worksheet, row, priceCol);
                if (string.IsNullOrWhiteSpace(priceRaw))
                {
                    errors.Add(new XmlImportError { Row = dataRow, Field = "Price", Message = "Price is required." });
                    rowValid = false;
                }
                else if (!decimal.TryParse(priceRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0m)
                {
                    errors.Add(new XmlImportError { Row = dataRow, Field = "Price", Message = "Price must be a valid decimal greater than 0." });
                    rowValid = false;
                }
            }

            if (mode == ImportMode.Stock)
            {
                var stockRaw = GetCellString(worksheet, row, stockCol);
                if (string.IsNullOrWhiteSpace(stockRaw))
                {
                    errors.Add(new XmlImportError { Row = dataRow, Field = "Stock", Message = "Stock is required." });
                    rowValid = false;
                }
                else if (!int.TryParse(stockRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock) || stock < 0)
                {
                    errors.Add(new XmlImportError { Row = dataRow, Field = "Stock", Message = "Stock must be a valid non-negative integer." });
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
            TotalRows = dataRow,
            SuccessCount = successCount,
            FailedCount = dataRow - successCount,
            Errors = errors
        };
    }

    private static Dictionary<string, int> BuildHeaderMap(ExcelWorksheet worksheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (worksheet.Dimension == null)
            return map;

        int lastCol = worksheet.Dimension.End.Column;
        for (int col = 1; col <= lastCol; col++)
        {
            var headerValue = worksheet.Cells[1, col].Text?.Trim();
            if (!string.IsNullOrWhiteSpace(headerValue) && !map.ContainsKey(headerValue))
            {
                map[headerValue] = col;
            }
        }

        return map;
    }

    private static int FindColumn(Dictionary<string, int> headerMap, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (headerMap.TryGetValue(candidate, out int col))
                return col;
        }

        return -1;
    }

    private static string? GetCellString(ExcelWorksheet worksheet, int row, int col)
    {
        if (col < 0)
            return null;
        var val = worksheet.Cells[row, col].Value;
        if (val == null)
            return null;
        return val.ToString()?.Trim();
    }

    private static bool IsRowEmpty(ExcelWorksheet worksheet, int row, int lastCol)
    {
        for (int col = 1; col <= lastCol; col++)
        {
            var val = worksheet.Cells[row, col].Value;
            if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                return false;
        }

        return true;
    }
}
