using System.Globalization;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace MesTech.Infrastructure.Services;

public sealed class ExcelExportService : IExcelExportService
{
    static ExcelExportService()
    {
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
    }

    public async Task<Stream> ExportProductsAsync(IEnumerable<ProductExportDto> products, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(products);
        return await Task.Run(() => BuildProductsExcel(products), ct).ConfigureAwait(false);
    }

    public async Task<Stream> ExportOrdersAsync(IEnumerable<OrderExportDto> orders, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(orders);
        return await Task.Run(() => BuildOrdersExcel(orders), ct).ConfigureAwait(false);
    }

    public async Task<Stream> ExportStockAsync(IEnumerable<StockExportDto> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        return await Task.Run(() => BuildStockExcel(items), ct).ConfigureAwait(false);
    }

    public async Task<Stream> ExportProfitabilityAsync(IEnumerable<ProfitabilityExportDto> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        return await Task.Run(() => BuildProfitabilityExcel(items), ct).ConfigureAwait(false);
    }

    private static MemoryStream BuildProductsExcel(IEnumerable<ProductExportDto> products)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Products");

        // Header row
        ws.Cells[1, 1].Value = "SKU";
        ws.Cells[1, 2].Value = "Name";
        ws.Cells[1, 3].Value = "Price";
        ws.Cells[1, 4].Value = "Stock";
        ws.Cells[1, 5].Value = "Category";
        ws.Cells[1, 6].Value = "Barcode";
        MakeBold(ws, 1, 6);

        int row = 2;
        foreach (var p in products)
        {
            ws.Cells[row, 1].Value = p.Sku;
            ws.Cells[row, 2].Value = p.Name;
            ws.Cells[row, 3].Value = p.Price.ToString("F2", CultureInfo.InvariantCulture);
            ws.Cells[row, 4].Value = p.Stock.ToString(CultureInfo.InvariantCulture);
            ws.Cells[row, 5].Value = p.Category ?? string.Empty;
            ws.Cells[row, 6].Value = p.Barcode ?? string.Empty;
            row++;
        }

        return SaveToStream(package);
    }

    private static MemoryStream BuildOrdersExcel(IEnumerable<OrderExportDto> orders)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Orders");

        // Header row
        ws.Cells[1, 1].Value = "OrderNumber";
        ws.Cells[1, 2].Value = "CustomerName";
        ws.Cells[1, 3].Value = "OrderDate";
        ws.Cells[1, 4].Value = "TotalAmount";
        ws.Cells[1, 5].Value = "Status";
        ws.Cells[1, 6].Value = "TrackingNumber";
        MakeBold(ws, 1, 6);

        int row = 2;
        foreach (var o in orders)
        {
            ws.Cells[row, 1].Value = o.OrderNumber;
            ws.Cells[row, 2].Value = o.CustomerName;
            ws.Cells[row, 3].Value = o.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ws.Cells[row, 4].Value = o.TotalAmount.ToString("F2", CultureInfo.InvariantCulture);
            ws.Cells[row, 5].Value = o.Status;
            ws.Cells[row, 6].Value = o.TrackingNumber ?? string.Empty;
            row++;
        }

        return SaveToStream(package);
    }

    private static MemoryStream BuildStockExcel(IEnumerable<StockExportDto> items)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Stock");

        // Header row
        ws.Cells[1, 1].Value = "SKU";
        ws.Cells[1, 2].Value = "Name";
        ws.Cells[1, 3].Value = "Stock";
        MakeBold(ws, 1, 3);

        int row = 2;
        foreach (var item in items)
        {
            ws.Cells[row, 1].Value = item.Sku;
            ws.Cells[row, 2].Value = item.Name;
            ws.Cells[row, 3].Value = item.Stock.ToString(CultureInfo.InvariantCulture);
            row++;
        }

        return SaveToStream(package);
    }

    private static MemoryStream BuildProfitabilityExcel(IEnumerable<ProfitabilityExportDto> items)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Profitability");

        // Header row
        ws.Cells[1, 1].Value = "SKU";
        ws.Cells[1, 2].Value = "Name";
        ws.Cells[1, 3].Value = "Revenue";
        ws.Cells[1, 4].Value = "Cost";
        ws.Cells[1, 5].Value = "Profit";
        ws.Cells[1, 6].Value = "MarginPercent";
        MakeBold(ws, 1, 6);

        int row = 2;
        foreach (var item in items)
        {
            ws.Cells[row, 1].Value = item.Sku;
            ws.Cells[row, 2].Value = item.Name;
            ws.Cells[row, 3].Value = item.Revenue.ToString("F2", CultureInfo.InvariantCulture);
            ws.Cells[row, 4].Value = item.Cost.ToString("F2", CultureInfo.InvariantCulture);
            ws.Cells[row, 5].Value = item.Profit.ToString("F2", CultureInfo.InvariantCulture);
            ws.Cells[row, 6].Value = item.MarginPercent.ToString("F2", CultureInfo.InvariantCulture);
            row++;
        }

        return SaveToStream(package);
    }

    private static void MakeBold(ExcelWorksheet ws, int row, int lastCol)
    {
        for (int col = 1; col <= lastCol; col++)
        {
            ws.Cells[row, col].Style.Font.Bold = true;
        }
    }

    private static MemoryStream SaveToStream(ExcelPackage package)
    {
        var ms = new MemoryStream();
        package.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}
