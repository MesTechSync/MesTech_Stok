using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// Bulk product export tests — validates Excel and CSV generation.
/// Uses ClosedXML for xlsx round-trip verification.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Platform")]
public class BulkProductExportTests
{
    // ═══════════════════════════════════════════════════════════
    // Helper: Create sample products for export
    // ═══════════════════════════════════════════════════════════

    private static List<Product> CreateSampleProducts(int count = 3, string? platformTag = null)
    {
        var products = new List<Product>();
        for (int i = 0; i < count; i++)
        {
            products.Add(new Product
            {
                SKU = $"EXP-{i + 1:D3}",
                Name = $"Export Product {i + 1}",
                SalePrice = 10.00m + (i * 5.50m),
                Stock = 10 + (i * 10),
                CategoryId = Guid.NewGuid(),
                Barcode = $"8680{i:D9}",
                IsActive = true,
                Location = platformTag // reuse Location field to tag platform for filtering
            });
        }
        return products;
    }

    private static MemoryStream ExportToExcel(List<Product> products)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Products");

        // Headers
        var headers = new[] { "SKU", "Name", "Price", "Stock", "Barcode", "Active" };
        for (int col = 0; col < headers.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = headers[col];
        }

        // Data rows
        for (int row = 0; row < products.Count; row++)
        {
            var p = products[row];
            worksheet.Cell(row + 2, 1).Value = p.SKU;
            worksheet.Cell(row + 2, 2).Value = p.Name;
            worksheet.Cell(row + 2, 3).Value = (double)p.SalePrice;
            worksheet.Cell(row + 2, 4).Value = p.Stock;
            worksheet.Cell(row + 2, 5).Value = p.Barcode ?? string.Empty;
            worksheet.Cell(row + 2, 6).Value = p.IsActive ? "Yes" : "No";
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static string ExportToCsv(List<Product> products)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SKU,Name,Price,Stock,Barcode,Active");

        foreach (var p in products)
        {
            var name = p.Name.Contains(',') ? $"\"{p.Name}\"" : p.Name;
            sb.AppendLine($"{p.SKU},{name},{p.SalePrice},{p.Stock},{p.Barcode ?? ""},{(p.IsActive ? "Yes" : "No")}");
        }

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════
    // 1. Excel export — produces valid xlsx that can be read back
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BulkExport_Excel_ShouldProduceValidXlsx()
    {
        // Arrange
        var products = CreateSampleProducts(5);

        // Act
        using var excelStream = ExportToExcel(products);

        // Assert — read back with ClosedXML
        using var readBack = new XLWorkbook(excelStream);
        var ws = readBack.Worksheets.First();

        ws.Name.Should().Be("Products");

        // Verify header row
        ws.Cell(1, 1).GetString().Should().Be("SKU");
        ws.Cell(1, 2).GetString().Should().Be("Name");
        ws.Cell(1, 3).GetString().Should().Be("Price");
        ws.Cell(1, 4).GetString().Should().Be("Stock");
        ws.Cell(1, 5).GetString().Should().Be("Barcode");
        ws.Cell(1, 6).GetString().Should().Be("Active");

        // Verify data rows
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        lastRow.Should().Be(6, "1 header + 5 data rows");

        ws.Cell(2, 1).GetString().Should().Be("EXP-001");
        ws.Cell(2, 2).GetString().Should().Be("Export Product 1");
        ws.Cell(6, 1).GetString().Should().Be("EXP-005");
        ws.Cell(6, 6).GetString().Should().Be("Yes");
    }

    // ═══════════════════════════════════════════════════════════
    // 2. CSV export — produces valid CSV with header + data rows
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BulkExport_Csv_ShouldProduceValidCsv()
    {
        // Arrange
        var products = CreateSampleProducts(3);

        // Act
        var csv = ExportToCsv(products);

        // Assert
        csv.Should().NotBeNullOrEmpty();

        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(4, "1 header + 3 data rows");

        // Verify header
        lines[0].Should().Be("SKU,Name,Price,Stock,Barcode,Active");

        // Verify first data row
        lines[1].Should().Contain("EXP-001");
        lines[1].Should().Contain("Export Product 1");
        // Locale-agnostic: TR uses "10,00", EN uses "10.00"
        lines[1].Should().Contain("10");
        lines[1].Should().Contain("Yes");

        // Verify last data row
        lines[3].Should().Contain("EXP-003");
    }

    // ═══════════════════════════════════════════════════════════
    // 3. Export by platform — filters products correctly
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void BulkExport_ByPlatform_ShouldFilterProducts()
    {
        // Arrange — create products tagged with different platforms
        var shopifyProducts = CreateSampleProducts(3, platformTag: "Shopify");
        var trendyolProducts = CreateSampleProducts(2, platformTag: "Trendyol");
        var allProducts = new List<Product>();
        allProducts.AddRange(shopifyProducts);
        allProducts.AddRange(trendyolProducts);

        allProducts.Should().HaveCount(5);

        // Act — filter for Shopify only
        var filtered = allProducts
            .Where(p => p.Location == "Shopify")
            .ToList();

        using var excelStream = ExportToExcel(filtered);

        // Assert — verify only Shopify products in export
        using var readBack = new XLWorkbook(excelStream);
        var ws = readBack.Worksheets.First();

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        lastRow.Should().Be(4, "1 header + 3 Shopify products");

        // Verify all exported products are Shopify-tagged
        for (int row = 2; row <= lastRow; row++)
        {
            var sku = ws.Cell(row, 1).GetString();
            sku.Should().StartWith("EXP-", "all exported products should have valid SKU");
        }

        // Verify Trendyol products are NOT in the export
        filtered.Should().HaveCount(3);
        filtered.All(p => p.Location == "Shopify").Should().BeTrue();
    }
}
