using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.FeedParsers;

namespace MesTech.Tests.Unit.Infrastructure.FeedParsers;

/// <summary>
/// CsvFeedParser birim testleri — ENT-DROP-SENTEZ-001 DEV5 Sprint A.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Parser", "Csv")]
public class CsvFeedParserTests
{
    private readonly CsvFeedParser _sut = new();
    private static readonly FeedFieldMapping DefaultMapping = new(null, null, null, null, null, null, null, null);

    // ── Yardımcı metotlar ─────────────────────────────────────────────────────

    private static Stream ToStream(string csv, Encoding? encoding = null) =>
        new MemoryStream((encoding ?? Encoding.UTF8).GetBytes(csv));

    // ── Test 1: Başlıklı geçerli CSV — ürünler ayrıştırılmalı ────────────────

    [Fact]
    public async Task ParseAsync_ValidCsvWithHeader_ReturnsParsedProducts()
    {
        var csv = "sku,name,price,quantity\nSKU-001,Test Ürün,99.90,10\nSKU-002,Diğer Ürün,49.50,5";
        using var stream = ToStream(csv);

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

    // ── Test 2: Tırnaklı alan (içinde virgül) — doğru ayrıştırılmalı ─────────

    [Fact]
    public async Task ParseAsync_QuotedFieldsWithComma_ParsesCorrectly()
    {
        var csv = "sku,name,price\nSKU-001,\"Ürün, Özel İsim\",75.00";
        using var stream = ToStream(csv);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].Name.Should().Be("Ürün, Özel İsim");
        result.Products[0].Price.Should().Be(75.00m);
    }

    // ── Test 3: SKU kolonu eksik — satır atlanmalı ───────────────────────────

    [Fact]
    public async Task ParseAsync_MissingSkuColumn_SkipsRow()
    {
        var csv = "name,price\nSku Olmayan Ürün,50.00";
        using var stream = ToStream(csv);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Contains("SKU and Barcode both missing"));
    }

    // ── Test 4: Boş CSV — boş sonuç ve hata ─────────────────────────────────

    [Fact]
    public async Task ParseAsync_EmptyCsv_ReturnsEmptyResult()
    {
        using var stream = ToStream(string.Empty);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.TotalParsed.Should().Be(0);
        result.Errors.Should().ContainSingle(e => e.Contains("Empty feed"));
    }

    // ── Test 5: Bilinmeyen sütunlar — extra'ya gitmeli ────────────────────────

    [Fact]
    public async Task ParseAsync_ExtraUnknownColumns_StoredInExtras()
    {
        var csv = "sku,name,price,renk,agirlik\nSKU-001,Test,99.90,Kırmızı,1.5";
        using var stream = ToStream(csv);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        var product = result.Products[0];
        product.ExtraFields.Should().ContainKey("renk");
        product.ExtraFields["renk"].Should().Be("Kırmızı");
        product.ExtraFields.Should().ContainKey("agirlik");
    }

    // ── Test 6: Tab ayırıcı — ayrıştırılamamalı (parser yalnızca virgül tanır) ─

    [Fact]
    public async Task ParseAsync_TabDelimited_SkipsRowsDueToSingleColumn()
    {
        // CsvFeedParser yalnızca virgülü destekler; tab'lı veri tek sütun gibi gelir
        var csv = "sku\tname\tprice\nSKU-001\tTest Ürün\t99.90";
        using var stream = ToStream(csv);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        // Başlık satırı tek sütun ("sku\tname\tprice") olarak okunur;
        // ürün satırında da SKU ve barcode bulunamaz → atlanır
        result.Products.Should().BeEmpty();
    }

    // ── Test 7: Türkçe karakterler — doğru işlenmeli ─────────────────────────

    [Fact]
    public async Task ParseAsync_TurkishCharacters_HandlesCorrectly()
    {
        var csv = "sku,name,price\nTR-001,Çiçek Desenli Ürün,150.00\nTR-002,Şişe & Şeker Seti,200.00";
        using var stream = ToStream(csv, Encoding.UTF8);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(2);
        result.Products[0].Name.Should().Be("Çiçek Desenli Ürün");
        result.Products[1].Name.Should().Be("Şişe & Şeker Seti");
    }

    // ── Test 8: Ondalık nokta ile fiyat ayrıştırma ───────────────────────────

    [Fact]
    public async Task ParseAsync_PriceWithDecimalPoint_ParsesCorrectly()
    {
        var csv = "sku,price\nSKU-001,1234.56";
        using var stream = ToStream(csv);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].Price.Should().Be(1234.56m);
    }

    // ── Test 9: Ondalık virgül ile fiyat — ayrıştırılamaz, null dönmeli ───────

    [Fact]
    public async Task ParseAsync_PriceWithCommaDecimal_ParsesAsNull()
    {
        // Virgül ayırıcı — "1.234,56" csv ayrıştırıcısında iki alan olarak okunur
        // InvariantCulture ile "1.234" parse edilir (bin ayırıcı yoksayılır)
        // ya da tamamen null döner. Her iki durumda ürün kabul edilmeli (SKU var).
        var csv = "sku,price\nSKU-001,\"1.234,56\"";
        using var stream = ToStream(csv);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        // Virgüllü ondalık ayrıştırılamaz → Price null beklenir
        result.Products[0].Price.Should().BeNull();
    }

    // ── Test 10: Başlık büyük/küçük harfe duyarsız eşleme ────────────────────

    [Fact]
    public async Task ParseAsync_HeaderCaseInsensitive_MapsCorrectly()
    {
        var csv = "SKU,NAME,PRICE,QUANTITY\nSKU-001,BÜYÜK HARF ÜRÜN,55.00,20";
        using var stream = ToStream(csv);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].SKU.Should().Be("SKU-001");
        result.Products[0].Name.Should().Be("BÜYÜK HARF ÜRÜN");
        result.Products[0].Quantity.Should().Be(20);
    }

    // ── Test 11: SupportedFormat — Csv dönmeli ────────────────────────────────

    [Fact]
    public void SupportedFormat_ReturnsCsv()
    {
        _sut.SupportedFormat.Should().Be(FeedFormat.Csv);
    }

    // ── Test 12: Yalnızca başlık satırı — 0 ürün ─────────────────────────────

    [Fact]
    public async Task ParseAsync_HeaderOnlyNoDataRows_ReturnsEmptyProducts()
    {
        var csv = "sku,name,price";
        using var stream = ToStream(csv);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.TotalParsed.Should().Be(0);
        result.SkippedCount.Should().Be(0);
    }

    // ── Test 13: Boş satırlar — atlanmalı ────────────────────────────────────

    [Fact]
    public async Task ParseAsync_BlankLines_AreSkipped()
    {
        var csv = "sku,name,price\nSKU-001,Ürün A,10.00\n\n\nSKU-002,Ürün B,20.00";
        using var stream = ToStream(csv);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(2);
        result.SkippedCount.Should().Be(0);
    }

    // ── Test 14: ValidateAsync — geçerli CSV ─────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ValidCsv_ReturnsIsValidTrue()
    {
        var csv = "sku,name,price\nSKU-001,Ürün,10.00\nSKU-002,Ürün 2,20.00";
        using var stream = ToStream(csv);

        var result = await _sut.ValidateAsync(stream);

        result.IsValid.Should().BeTrue();
        result.EstimatedProductCount.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }
}
