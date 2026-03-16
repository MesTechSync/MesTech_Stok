using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Infrastructure.Services;
using OfficeOpenXml;
using System.IO;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for ExcelExportService — G6 C-04.
/// </summary>
[Trait("Category", "Unit")]
public class ExcelExportServiceTests
{
    static ExcelExportServiceTests()
    {
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
    }

    private readonly ExcelExportService _sut = new();

    // ── Helper: read back exported stream with EPPlus ─────────────────────────

    private static ExcelWorksheet OpenFirstSheet(Stream stream)
    {
        stream.Position = 0;
        var package = new ExcelPackage(stream);
        return package.Workbook.Worksheets[0];
    }

    // ── Test 1: ExportProducts — 3 items → header + 3 data rows ─────────────

    [Fact]
    public async Task ExportProductsAsync_ThreeProducts_ReturnsValidExcel()
    {
        var products = new[]
        {
            new ProductExportDto("SKU-001", "Product A", 10.99m, 50, "Category1", "1234567890"),
            new ProductExportDto("SKU-002", "Product B", 25.00m, 100, null, null),
            new ProductExportDto("SKU-003", "Product C", 5.50m, 10, "Category2", "0987654321"),
        };

        var stream = await _sut.ExportProductsAsync(products);

        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);

        var ws = OpenFirstSheet(stream);
        ws.Cells[1, 1].Text.Should().Be("SKU");
        ws.Cells[1, 1].Style.Font.Bold.Should().BeTrue();
        // 1 header row + 3 data rows = last data row is 4
        ws.Dimension.End.Row.Should().Be(4);
    }

    // ── Test 2: ExportOrders — 2 orders → header + 2 data rows ──────────────

    [Fact]
    public async Task ExportOrdersAsync_TwoOrders_ReturnsValidExcel()
    {
        var orders = new[]
        {
            new OrderExportDto("ORD-001", "Ali Veli", new DateTime(2026, 1, 15), 150.00m, "Shipped", "TRK123"),
            new OrderExportDto("ORD-002", "Mehmet Can", new DateTime(2026, 2, 20), 89.99m, "Pending", null),
        };

        var stream = await _sut.ExportOrdersAsync(orders);

        stream.Should().NotBeNull();

        var ws = OpenFirstSheet(stream);
        ws.Cells[1, 1].Text.Should().Be("OrderNumber");
        ws.Cells[1, 1].Style.Font.Bold.Should().BeTrue();
        ws.Dimension.End.Row.Should().Be(3); // header + 2 data rows
    }

    // ── Test 3: ExportStock — 2 items → header "SKU", no "Price" column ─────

    [Fact]
    public async Task ExportStockAsync_TwoItems_ReturnsValidExcelWithNoPrice()
    {
        var items = new[]
        {
            new StockExportDto("SKU-A", "Item A", 30),
            new StockExportDto("SKU-B", "Item B", 0),
        };

        var stream = await _sut.ExportStockAsync(items);

        stream.Should().NotBeNull();

        var ws = OpenFirstSheet(stream);
        ws.Cells[1, 1].Text.Should().Be("SKU");
        ws.Cells[1, 2].Text.Should().Be("Name");
        ws.Cells[1, 3].Text.Should().Be("Stock");
        // Only 3 columns — no "Price" column
        ws.Dimension.End.Column.Should().Be(3);
        ws.Dimension.End.Row.Should().Be(3); // header + 2 data rows
    }

    // ── Test 4: ExportProfitability — 2 items → header "MarginPercent" ───────

    [Fact]
    public async Task ExportProfitabilityAsync_TwoItems_ReturnsValidExcel()
    {
        var items = new[]
        {
            new ProfitabilityExportDto("SKU-001", "Product A", 200m, 120m, 80m, 40.00m),
            new ProfitabilityExportDto("SKU-002", "Product B", 500m, 350m, 150m, 30.00m),
        };

        var stream = await _sut.ExportProfitabilityAsync(items);

        stream.Should().NotBeNull();

        var ws = OpenFirstSheet(stream);
        ws.Cells[1, 6].Text.Should().Be("MarginPercent");
        ws.Cells[1, 6].Style.Font.Bold.Should().BeTrue();
        ws.Dimension.End.Row.Should().Be(3); // header + 2 data rows
    }

    // ── Test 5: Empty collection → valid Excel with 1 header row, 0 data rows ─

    [Fact]
    public async Task ExportProductsAsync_EmptyCollection_ReturnsHeaderOnlyExcel()
    {
        var stream = await _sut.ExportProductsAsync(Array.Empty<ProductExportDto>());

        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);

        var ws = OpenFirstSheet(stream);
        ws.Cells[1, 1].Text.Should().Be("SKU");
        // Only the header row exists
        ws.Dimension.End.Row.Should().Be(1);
    }

    // ── Test 6: Null argument → ArgumentNullException ─────────────────────────

    [Fact]
    public async Task ExportProductsAsync_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.ExportProductsAsync(null!));
    }

    // ── Test 7: Null argument for Orders → ArgumentNullException ─────────────

    [Fact]
    public async Task ExportOrdersAsync_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.ExportOrdersAsync(null!));
    }

    // ── Test 8: Null argument for Stock → ArgumentNullException ──────────────

    [Fact]
    public async Task ExportStockAsync_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.ExportStockAsync(null!));
    }

    // ── Test 9: Null argument for Profitability → ArgumentNullException ───────

    [Fact]
    public async Task ExportProfitabilityAsync_NullArgument_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.ExportProfitabilityAsync(null!));
    }

    // ── Test 10: Empty orders collection → valid Excel header only ────────────

    [Fact]
    public async Task ExportOrdersAsync_EmptyCollection_ReturnsHeaderOnlyExcel()
    {
        var stream = await _sut.ExportOrdersAsync(Array.Empty<OrderExportDto>());

        stream.Should().NotBeNull();
        var ws = OpenFirstSheet(stream);
        ws.Cells[1, 1].Text.Should().Be("OrderNumber");
        ws.Dimension.End.Row.Should().Be(1);
    }

    // ── Test 11: DateTime formatting is yyyy-MM-dd ────────────────────────────

    [Fact]
    public async Task ExportOrdersAsync_DateFormatting_IsIso8601()
    {
        var orders = new[]
        {
            new OrderExportDto("ORD-100", "Test Customer", new DateTime(2026, 3, 10), 99.99m, "Processing", null),
        };

        var stream = await _sut.ExportOrdersAsync(orders);

        var ws = OpenFirstSheet(stream);
        ws.Cells[2, 3].Text.Should().Be("2026-03-10");
    }

    // ── Test 12: Decimal values use InvariantCulture (dot separator) ──────────

    [Fact]
    public async Task ExportProfitabilityAsync_DecimalValues_UseDotSeparator()
    {
        var items = new[]
        {
            new ProfitabilityExportDto("SKU-DEC", "Decimal Test", 1234.56m, 789.01m, 445.55m, 36.10m),
        };

        var stream = await _sut.ExportProfitabilityAsync(items);

        var ws = OpenFirstSheet(stream);
        ws.Cells[2, 3].Text.Should().Be("1234.56");
        ws.Cells[2, 6].Text.Should().Be("36.10");
    }
}
