using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.FeedParsers;

namespace MesTech.Tests.Unit.Infrastructure.FeedParsers;

/// <summary>
/// XmlFeedParser birim testleri — ENT-DROP-SENTEZ-001 DEV5 Sprint A.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Parser", "Xml")]
public class XmlFeedParserTests
{
    private readonly XmlFeedParser _sut = new();
    private static readonly FeedFieldMapping DefaultMapping = new(null, null, null, null, null, null, null, null);

    // ── Yardımcı metotlar ─────────────────────────────────────────────────────

    private static Stream ToStream(string xml) =>
        new MemoryStream(Encoding.UTF8.GetBytes(xml));

    private static string BuildXml(params string[] productBlocks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<products>");
        foreach (var block in productBlocks)
            sb.AppendLine(block);
        sb.AppendLine("</products>");
        return sb.ToString();
    }

    private static string ProductBlock(
        string? sku = "SKU-001",
        string? barcode = null,
        string? name = "Test Ürün",
        string? price = "99.90",
        string? quantity = "10",
        string? category = null,
        string? extra = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("  <product>");
        if (sku != null)       sb.AppendLine($"    <sku>{sku}</sku>");
        if (barcode != null)   sb.AppendLine($"    <barcode>{barcode}</barcode>");
        if (name != null)      sb.AppendLine($"    <name>{name}</name>");
        if (price != null)     sb.AppendLine($"    <price>{price}</price>");
        if (quantity != null)  sb.AppendLine($"    <quantity>{quantity}</quantity>");
        if (category != null)  sb.AppendLine($"    <category>{category}</category>");
        if (extra != null)     sb.AppendLine(extra);
        sb.AppendLine("  </product>");
        return sb.ToString();
    }

    // ── Test 1: Geçerli XML — SKU ile ayrıştırma ─────────────────────────────

    [Fact]
    public async Task ParseAsync_ValidXmlWithSku_ReturnsParsedProduct()
    {
        var xml = BuildXml(ProductBlock(sku: "SKU-001", name: "Test Ürün", price: "99.90", quantity: "5"));
        using var stream = ToStream(xml);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].SKU.Should().Be("SKU-001");
        result.Products[0].Name.Should().Be("Test Ürün");
        result.Products[0].Price.Should().Be(99.90m);
        result.Products[0].Quantity.Should().Be(5);
        result.SkippedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 2: Geçerli XML — barkod ile ayrıştırma ──────────────────────────

    [Fact]
    public async Task ParseAsync_ValidXmlWithBarcode_ReturnsParsedProduct()
    {
        var xml = BuildXml(ProductBlock(sku: null, barcode: "8680000123456", name: "Barkodlu Ürün", price: "49.99"));
        using var stream = ToStream(xml);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].SKU.Should().BeNull();
        result.Products[0].Barcode.Should().Be("8680000123456");
        result.SkippedCount.Should().Be(0);
    }

    // ── Test 3: SKU ve barkod eksik — satır atlanmalı ────────────────────────

    [Fact]
    public async Task ParseAsync_MissingSkuAndBarcode_SkipsRow()
    {
        var xml = BuildXml(ProductBlock(sku: null, barcode: null, name: "Kimliksiz Ürün"));
        using var stream = ToStream(xml);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Contains("SKU and Barcode both missing"));
    }

    // ── Test 4: Boş XML — boş sonuç dönmeli ─────────────────────────────────

    [Fact]
    public async Task ParseAsync_EmptyXml_ReturnsEmptyResult()
    {
        var xml = "<products></products>";
        using var stream = ToStream(xml);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.TotalParsed.Should().Be(0);
        result.SkippedCount.Should().Be(0);
    }

    // ── Test 5: Bozuk XML — hata kayıtlı boş sonuç dönmeli ──────────────────

    [Fact]
    public async Task ParseAsync_MalformedXml_ReturnsErrorInResult()
    {
        var malformed = "<products><product><sku>SKU-1</sku></product_WRONG></products>";
        using var stream = ToStream(malformed);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        // Bozuk XML ya hata yayar ya da ürünü kaçırır; her iki durumda hata listesi dolu olmalı
        result.Errors.Should().NotBeEmpty();
    }

    // ── Test 6: Fiyat markup ile ayrıştırma ─────────────────────────────────

    [Fact]
    public async Task ParseAsync_WithPriceField_ParsesPriceCorrectly()
    {
        var xml = BuildXml(ProductBlock(sku: "MRK-001", price: "199.99"));
        using var stream = ToStream(xml);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].Price.Should().Be(199.99m);
    }

    // ── Test 7: Sıfır stoklu ürün — sonuçta yer almalı ──────────────────────

    [Fact]
    public async Task ParseAsync_ZeroStockProduct_IsIncludedInResult()
    {
        var xml = BuildXml(ProductBlock(sku: "ZERO-001", quantity: "0"));
        using var stream = ToStream(xml);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].Quantity.Should().Be(0);
    }

    // ── Test 8: Özel alan eşlemesi ────────────────────────────────────────────

    [Fact]
    public async Task ParseAsync_CustomFieldMapping_MapsCorrectly()
    {
        var xml = @"<products>
  <product>
    <stok_kodu>OZEL-001</stok_kodu>
    <urun_adi>Özel Alan Ürün</urun_adi>
    <liste_fiyati>75.00</liste_fiyati>
  </product>
</products>";
        using var stream = ToStream(xml);
        var mapping = new FeedFieldMapping(
            SkuField: "stok_kodu",
            BarcodeField: null,
            NameField: "urun_adi",
            PriceField: "liste_fiyati",
            QuantityField: null,
            CategoryField: null,
            ImageField: null,
            DescriptionField: null);

        var result = await _sut.ParseAsync(stream, mapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].SKU.Should().Be("OZEL-001");
        result.Products[0].Name.Should().Be("Özel Alan Ürün");
        result.Products[0].Price.Should().Be(75.00m);
    }

    // ── Test 9: Büyük XML — tüm ürünler ayrıştırılmalı ──────────────────────

    [Fact]
    public async Task ParseAsync_LargeXml_ParsesAllProducts()
    {
        const int productCount = 500;
        var blocks = Enumerable.Range(1, productCount)
            .Select(i => ProductBlock(sku: $"SKU-{i:D4}", price: $"{i}.99", quantity: i.ToString()))
            .ToArray();
        var xml = BuildXml(blocks);
        using var stream = ToStream(xml);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(productCount);
        result.SkippedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 10: Tekrar eden SKU — her ikisi de sonuçta yer almalı ───────────

    [Fact]
    public async Task ParseAsync_DuplicateSku_BothIncludedInResult()
    {
        var xml = BuildXml(
            ProductBlock(sku: "DUPE-001", name: "Ürün A"),
            ProductBlock(sku: "DUPE-001", name: "Ürün B"));
        using var stream = ToStream(xml);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        // Parser duplicate kontrolü yapmaz, her ikisi de eklenir
        result.Products.Should().HaveCount(2);
        result.Products.Should().AllSatisfy(p => p.SKU.Should().Be("DUPE-001"));
    }

    // ── Test 11: SupportedFormat — Xml dönmeli ───────────────────────────────

    [Fact]
    public void SupportedFormat_ReturnsXml()
    {
        _sut.SupportedFormat.Should().Be(FeedFormat.Xml);
    }

    // ── Test 12: ExtraFields — bilinmeyen alanlar extra'ya gitmeli ────────────

    [Fact]
    public async Task ParseAsync_UnknownFields_StoredInExtraFields()
    {
        var xml = BuildXml(ProductBlock(
            sku: "EXTRA-001",
            extra: "    <renk>Kırmızı</renk>\n    <agirlik_kg>1.5</agirlik_kg>"));
        using var stream = ToStream(xml);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        var product = result.Products[0];
        product.ExtraFields.Should().ContainKey("renk");
        product.ExtraFields["renk"].Should().Be("Kırmızı");
    }

    // ── Test 13: ValidateAsync — geçerli XML ─────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ValidXml_ReturnsIsValidTrue()
    {
        var xml = BuildXml(ProductBlock(), ProductBlock(sku: "SKU-002"));
        using var stream = ToStream(xml);

        var result = await _sut.ValidateAsync(stream);

        result.IsValid.Should().BeTrue();
        result.EstimatedProductCount.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }
}
