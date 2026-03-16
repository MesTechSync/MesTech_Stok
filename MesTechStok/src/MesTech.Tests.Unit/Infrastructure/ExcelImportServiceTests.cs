using FluentAssertions;
using MesTech.Infrastructure.Services;
using OfficeOpenXml;
using System.IO;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for ExcelImportService — G5 C-03.
/// </summary>
[Trait("Category", "Unit")]
public class ExcelImportServiceTests
{
    static ExcelImportServiceTests()
    {
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
    }

    private readonly ExcelImportService _sut = new();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Stream BuildProductSheet(IEnumerable<(string sku, string name, string price)> rows)
    {
        var ms = new MemoryStream();
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Products");
        ws.Cells[1, 1].Value = "SKU";
        ws.Cells[1, 2].Value = "Name";
        ws.Cells[1, 3].Value = "Price";
        int r = 2;
        foreach (var (sku, name, price) in rows)
        {
            ws.Cells[r, 1].Value = sku;
            ws.Cells[r, 2].Value = name;
            ws.Cells[r, 3].Value = price;
            r++;
        }

        pkg.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static Stream BuildStockSheet(IEnumerable<(string sku, string name, string stock)> rows)
    {
        var ms = new MemoryStream();
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Stock");
        ws.Cells[1, 1].Value = "SKU";
        ws.Cells[1, 2].Value = "Name";
        ws.Cells[1, 3].Value = "Stock";
        int r = 2;
        foreach (var (sku, name, stock) in rows)
        {
            ws.Cells[r, 1].Value = sku;
            ws.Cells[r, 2].Value = name;
            ws.Cells[r, 3].Value = stock;
            r++;
        }

        pkg.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static Stream BuildPriceSheet(IEnumerable<(string sku, string name, string price)> rows)
        => BuildProductSheet(rows); // same shape

    private static Stream BuildHeaderOnlySheet(string col1, string col2, string col3)
    {
        var ms = new MemoryStream();
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Sheet1");
        ws.Cells[1, 1].Value = col1;
        ws.Cells[1, 2].Value = col2;
        ws.Cells[1, 3].Value = col3;
        pkg.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    // ── Test 1: ImportProducts — 3 valid rows → SuccessCount = 3, 0 errors ──

    [Fact]
    public async Task ImportProductsAsync_ThreeValidRows_ReturnsSuccessCount3()
    {
        var stream = BuildProductSheet(new[]
        {
            ("SKU-001", "Product A", "10.99"),
            ("SKU-002", "Product B", "25.00"),
            ("SKU-003", "Product C", "5.50"),
        });

        var result = await _sut.ImportProductsAsync(stream);

        result.TotalRows.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.FailedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 2: ImportStock — 2 valid rows → SuccessCount = 2 ───────────────

    [Fact]
    public async Task ImportStockAsync_TwoValidRows_ReturnsSuccessCount2()
    {
        var stream = BuildStockSheet(new[]
        {
            ("SKU-A", "Item A", "100"),
            ("SKU-B", "Item B", "0"),
        });

        var result = await _sut.ImportStockAsync(stream);

        result.TotalRows.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 3: ImportPrices — 2 valid rows → SuccessCount = 2 ──────────────

    [Fact]
    public async Task ImportPricesAsync_TwoValidRows_ReturnsSuccessCount2()
    {
        var stream = BuildPriceSheet(new[]
        {
            ("SKU-X", "Item X", "99.99"),
            ("SKU-Y", "Item Y", "1.00"),
        });

        var result = await _sut.ImportPricesAsync(stream);

        result.TotalRows.Should().Be(2);
        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 4: Missing SKU → error with Field="SKU" ─────────────────────────

    [Fact]
    public async Task ImportProductsAsync_MissingSku_AddsSkuError()
    {
        var stream = BuildProductSheet(new[]
        {
            ("", "No SKU Product", "9.99"),
        });

        var result = await _sut.ImportProductsAsync(stream);

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().ContainSingle(e => e.Field == "SKU");
    }

    // ── Test 5: Duplicate SKU → error ────────────────────────────────────────

    [Fact]
    public async Task ImportProductsAsync_DuplicateSku_AddsError()
    {
        var stream = BuildProductSheet(new[]
        {
            ("DUPE-001", "Product 1", "10.00"),
            ("DUPE-001", "Product 2", "20.00"),
        });

        var result = await _sut.ImportProductsAsync(stream);

        result.SuccessCount.Should().Be(1);
        result.Errors.Should().ContainSingle(e =>
            e.Field == "SKU" && e.Message.Contains("Duplicate"));
    }

    // ── Test 6: Invalid price (negative / text) → error ─────────────────────

    [Fact]
    public async Task ImportProductsAsync_InvalidPrice_AddsError()
    {
        var stream = BuildProductSheet(new[]
        {
            ("SKU-NEG", "Item Neg", "-5.00"),
            ("SKU-TXT", "Item Txt", "not-a-price"),
        });

        var result = await _sut.ImportProductsAsync(stream);

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().AllSatisfy(e => e.Field.Should().Be("Price"));
    }

    // ── Test 7: Invalid stock (negative / text) → error ─────────────────────

    [Fact]
    public async Task ImportStockAsync_InvalidStock_AddsError()
    {
        var stream = BuildStockSheet(new[]
        {
            ("SKU-NEG", "Item Neg", "-1"),
            ("SKU-TXT", "Item Txt", "many"),
        });

        var result = await _sut.ImportStockAsync(stream);

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().AllSatisfy(e => e.Field.Should().Be("Stock"));
    }

    // ── Test 8: Null stream → ArgumentNullException ──────────────────────────

    [Fact]
    public async Task ImportProductsAsync_NullStream_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.ImportProductsAsync(null!));
    }

    // ── Test 9: Empty sheet (header only, no data rows) → TotalRows = 0 ─────

    [Fact]
    public async Task ImportProductsAsync_HeaderOnlySheet_ReturnsTotalRows0()
    {
        var stream = BuildHeaderOnlySheet("SKU", "Name", "Price");

        var result = await _sut.ImportProductsAsync(stream);

        result.TotalRows.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 10: Stock import — zero stock quantity is valid ─────────────────

    [Fact]
    public async Task ImportStockAsync_ZeroStock_IsValid()
    {
        var stream = BuildStockSheet(new[]
        {
            ("SKU-ZERO", "Zero Stock Item", "0"),
        });

        var result = await _sut.ImportStockAsync(stream);

        result.SuccessCount.Should().Be(1);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 11: Turkish column names ("Ad", "Fiyat", "Stok") are accepted ───

    [Fact]
    public async Task ImportProductsAsync_TurkishColumnNames_Accepted()
    {
        var ms = new MemoryStream();
        using (var pkg = new ExcelPackage())
        {
            var ws = pkg.Workbook.Worksheets.Add("Urunler");
            ws.Cells[1, 1].Value = "SKU";
            ws.Cells[1, 2].Value = "Ad";
            ws.Cells[1, 3].Value = "Fiyat";
            ws.Cells[2, 1].Value = "TR-001";
            ws.Cells[2, 2].Value = "Türkçe Ürün";
            ws.Cells[2, 3].Value = "49.90";
            pkg.SaveAs(ms);
        }

        ms.Position = 0;
        var result = await _sut.ImportProductsAsync(ms);

        result.SuccessCount.Should().Be(1);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 12: Missing Name column value → error with Field="Name" ─────────

    [Fact]
    public async Task ImportProductsAsync_MissingName_AddsNameError()
    {
        var stream = BuildProductSheet(new[]
        {
            ("SKU-NONAME", "", "15.00"),
        });

        var result = await _sut.ImportProductsAsync(stream);

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().ContainSingle(e => e.Field == "Name");
    }
}
