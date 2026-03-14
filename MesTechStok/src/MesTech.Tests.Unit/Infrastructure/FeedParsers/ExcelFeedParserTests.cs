using System.IO;
using ClosedXML.Excel;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.FeedParsers;

namespace MesTech.Tests.Unit.Infrastructure.FeedParsers;

/// <summary>
/// ExcelFeedParser birim testleri — ENT-DROP-SENTEZ-001 DEV5 Sprint A.
/// ClosedXML ile in-memory .xlsx dosyaları oluşturulur; disk işlemi yoktur.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Parser", "Excel")]
public class ExcelFeedParserTests
{
    private readonly ExcelFeedParser _sut = new();
    private static readonly FeedFieldMapping DefaultMapping = new(null, null, null, null, null, null, null, null);

    // ── Yardımcı metotlar ─────────────────────────────────────────────────────

    /// <summary>
    /// Standart sütun başlıkları ve verilerle in-memory Excel stream oluşturur.
    /// </summary>
    private static Stream BuildExcelStream(
        string[] headers,
        IEnumerable<string?[]> rows,
        string sheetName = "Sheet1")
    {
        var ms = new MemoryStream();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);

        // Başlık satırı
        for (var col = 0; col < headers.Length; col++)
            ws.Cell(1, col + 1).Value = headers[col];

        // Veri satırları
        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var col = 0; col < row.Length; col++)
                ws.Cell(rowIndex, col + 1).Value = row[col] ?? string.Empty;
            rowIndex++;
        }

        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static Stream BuildEmptyExcelStream()
    {
        var ms = new MemoryStream();
        using var wb = new XLWorkbook();
        wb.Worksheets.Add("Bos");
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    // ── Test 1: Geçerli Excel — ürünler ayrıştırılmalı ───────────────────────

    [Fact]
    public async Task ParseAsync_ValidExcel_ReturnsParsedProducts()
    {
        using var stream = BuildExcelStream(
            headers: ["sku", "name", "price", "quantity"],
            rows:
            [
                ["SKU-001", "Test Ürün", "99.90", "10"],
                ["SKU-002", "Diğer Ürün", "49.50", "5"],
            ]);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(2);
        result.Products[0].SKU.Should().Be("SKU-001");
        result.Products[0].Name.Should().Be("Test Ürün");
        result.Products[0].Price.Should().Be(99.90m);
        result.Products[0].Quantity.Should().Be(10);
        result.Products[1].SKU.Should().Be("SKU-002");
        result.SkippedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 2: Boş Excel (sadece başlık) — boş sonuç ───────────────────────

    [Fact]
    public async Task ParseAsync_EmptyExcel_ReturnsEmptyResult()
    {
        using var stream = BuildEmptyExcelStream();

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.TotalParsed.Should().Be(0);
    }

    // ── Test 3: Başlık ilk satırda — doğru algılanmalı ───────────────────────

    [Fact]
    public async Task ParseAsync_HeaderInFirstRow_DetectedCorrectly()
    {
        using var stream = BuildExcelStream(
            headers: ["sku", "name"],
            rows: [["HDR-001", "Başlık Testi"]]);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].SKU.Should().Be("HDR-001");
    }

    // ── Test 4: SKU kolonu eksik — satır atlanmalı ───────────────────────────

    [Fact]
    public async Task ParseAsync_MissingRequiredColumn_SkipsRow()
    {
        // Sadece 'name' ve 'price' sütunları var, SKU/barcode yok
        using var stream = BuildExcelStream(
            headers: ["name", "price"],
            rows: [["Ürün Adı", "55.00"]]);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Contains("SKU and Barcode both missing"));
    }

    // ── Test 5: Sayısal fiyat hücresi — ExcelFeedParser GetString() kullanır ──
    //   ClosedXML double hücresini GetString() ile "299.99" olarak döndürür.
    //   Ondalıklı değer için hücreye string atanır, ardından parser ayrıştırır.

    [Fact]
    public async Task ParseAsync_NumericPriceCell_ParsesCorrectly()
    {
        // ExcelFeedParser ws.Cell(row,col).GetString() kullanıyor.
        // ClosedXML ile integer değer (tamsayı) atandığında GetString() doğru çalışır.
        var ms = new MemoryStream();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sayısal");
        ws.Cell(1, 1).Value = "sku";
        ws.Cell(1, 2).Value = "price";
        ws.Cell(2, 1).Value = "NUM-001";
        ws.Cell(2, 2).Value = 300; // tamsayı sayısal değer
        wb.SaveAs(ms);
        ms.Position = 0;

        var result = await _sut.ParseAsync(ms, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].Price.Should().Be(300m);
    }

    // ── Test 6: String fiyat hücresi — doğru ayrıştırılmalı ─────────────────

    [Fact]
    public async Task ParseAsync_StringPriceCell_ParsesCorrectly()
    {
        using var stream = BuildExcelStream(
            headers: ["sku", "price"],
            rows: [["STR-001", "149.99"]]);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].Price.Should().Be(149.99m);
    }

    // ── Test 7: Boş veri satırı — atlanmalı ──────────────────────────────────

    [Fact]
    public async Task ParseAsync_EmptyRow_Skipped()
    {
        // Arada tamamen boş satır
        using var stream = BuildExcelStream(
            headers: ["sku", "name", "price"],
            rows:
            [
                ["SKU-001", "Ürün A", "10.00"],
                [null, null, null], // boş satır
                ["SKU-002", "Ürün B", "20.00"],
            ]);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        // Boş satır atlanır (SKU+barcode eksik)
        result.Products.Should().HaveCount(2);
        result.Products.Should().OnlyContain(p =>
            p.SKU == "SKU-001" || p.SKU == "SKU-002");
    }

    // ── Test 8: Ekstra kolonlar — extra'ya gitmeli ────────────────────────────

    [Fact]
    public async Task ParseAsync_ExtraColumns_StoredInExtras()
    {
        using var stream = BuildExcelStream(
            headers: ["sku", "name", "price", "renk", "malzeme"],
            rows: [["EXTRA-001", "Renkli Ürün", "89.90", "Mavi", "Pamuk"]]);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        var product = result.Products[0];
        product.ExtraFields.Should().ContainKey("renk");
        product.ExtraFields["renk"].Should().Be("Mavi");
        product.ExtraFields.Should().ContainKey("malzeme");
        product.ExtraFields["malzeme"].Should().Be("Pamuk");
    }

    // ── Test 9: SupportedFormat — Excel dönmeli ───────────────────────────────

    [Fact]
    public void SupportedFormat_ReturnsExcel()
    {
        _sut.SupportedFormat.Should().Be(FeedFormat.Excel);
    }

    // ── Test 10: Büyük/küçük harf başlık — eşleşmeli ────────────────────────

    [Fact]
    public async Task ParseAsync_HeaderCaseInsensitive_MapsCorrectly()
    {
        using var stream = BuildExcelStream(
            headers: ["SKU", "NAME", "PRICE"],
            rows: [["CASE-001", "Büyük Harf Başlık", "33.33"]]);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].SKU.Should().Be("CASE-001");
        result.Products[0].Name.Should().Be("Büyük Harf Başlık");
    }

    // ── Test 11: Çoklu ürün — tümü ayrıştırılmalı ────────────────────────────

    [Fact]
    public async Task ParseAsync_MultipleProducts_AllParsedCorrectly()
    {
        const int count = 50;
        var rows = Enumerable.Range(1, count)
            .Select(i => new string?[] { $"SKU-{i:D3}", $"Ürün {i}", $"{i * 10}.00", i.ToString() })
            .ToArray();

        using var stream = BuildExcelStream(
            headers: ["sku", "name", "price", "quantity"],
            rows: rows);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(count);
        result.SkippedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 12: ValidateAsync — geçerli Excel ───────────────────────────────

    [Fact]
    public async Task ValidateAsync_ValidExcel_ReturnsIsValidTrue()
    {
        using var stream = BuildExcelStream(
            headers: ["sku", "name"],
            rows:
            [
                ["SKU-001", "Ürün A"],
                ["SKU-002", "Ürün B"],
            ]);

        var result = await _sut.ValidateAsync(stream);

        result.IsValid.Should().BeTrue();
        result.EstimatedProductCount.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }
}
