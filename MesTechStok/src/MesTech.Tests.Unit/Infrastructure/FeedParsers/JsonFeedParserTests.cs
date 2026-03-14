using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.FeedParsers;

namespace MesTech.Tests.Unit.Infrastructure.FeedParsers;

/// <summary>
/// JsonFeedParser birim testleri — ENT-DROP-SENTEZ-001 DEV5 Sprint A.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Parser", "Json")]
public class JsonFeedParserTests
{
    private readonly JsonFeedParser _sut = new();
    private static readonly FeedFieldMapping DefaultMapping = new(null, null, null, null, null, null, null, null);

    // ── Yardımcı metotlar ─────────────────────────────────────────────────────

    private static Stream ToStream(string json) =>
        new MemoryStream(Encoding.UTF8.GetBytes(json));

    // ── Test 1: Kök dizi JSON — ürünler ayrıştırılmalı ───────────────────────

    [Fact]
    public async Task ParseAsync_RootArrayJson_ReturnsParsedProducts()
    {
        var json = """
            [
              { "sku": "SKU-001", "name": "Ürün A", "price": 99.90, "quantity": 10 },
              { "sku": "SKU-002", "name": "Ürün B", "price": 49.50, "quantity": 5  }
            ]
            """;
        using var stream = ToStream(json);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(2);
        result.Products[0].SKU.Should().Be("SKU-001");
        result.Products[0].Name.Should().Be("Ürün A");
        result.Products[0].Price.Should().Be(99.90m);
        result.Products[0].Quantity.Should().Be(10);
        result.Products[1].SKU.Should().Be("SKU-002");
        result.SkippedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 2: "products" anahtarı altında dizi — ürünler ayrıştırılmalı ────

    [Fact]
    public async Task ParseAsync_ProductsKeyJson_ReturnsParsedProducts()
    {
        var json = """
            {
              "total": 2,
              "products": [
                { "sku": "P-001", "name": "Ürün 1", "price": 25.00 },
                { "sku": "P-002", "name": "Ürün 2", "price": 35.00 }
              ]
            }
            """;
        using var stream = ToStream(json);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(2);
        result.Products[0].SKU.Should().Be("P-001");
        result.Products[1].SKU.Should().Be("P-002");
    }

    // ── Test 3: Boş dizi — boş sonuç ─────────────────────────────────────────

    [Fact]
    public async Task ParseAsync_EmptyArray_ReturnsEmptyResult()
    {
        var json = "[]";
        using var stream = ToStream(json);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.TotalParsed.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 4: Null değerler — graceful handle ───────────────────────────────

    [Fact]
    public async Task ParseAsync_NullValues_HandledGracefully()
    {
        var json = """
            [
              {
                "sku": "NULL-001",
                "name": null,
                "price": null,
                "quantity": null,
                "description": null
              }
            ]
            """;
        using var stream = ToStream(json);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        var product = result.Products[0];
        product.SKU.Should().Be("NULL-001");
        product.Name.Should().BeNull();
        product.Price.Should().BeNull();
        product.Quantity.Should().BeNull();
        result.SkippedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Test 5: SupportedFormat — Json dönmeli ───────────────────────────────

    [Fact]
    public void SupportedFormat_ReturnsJson()
    {
        _sut.SupportedFormat.Should().Be(FeedFormat.Json);
    }

    // ── Test 6: Bozuk JSON — hata mesajı dönmeli ─────────────────────────────

    [Fact]
    public async Task ParseAsync_InvalidJson_ReturnsErrorInResult()
    {
        var json = "{ this is not valid json !!!";
        using var stream = ToStream(json);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.Errors.Should().ContainSingle(e => e.Contains("Invalid JSON"));
    }

    // ── Test 7: SKU ve barcode eksik — satır atlanmalı ───────────────────────

    [Fact]
    public async Task ParseAsync_MissingSkuAndBarcode_SkipsItem()
    {
        var json = """
            [
              { "name": "Kimliksiz Ürün", "price": 10.00 }
            ]
            """;
        using var stream = ToStream(json);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Contains("SKU and Barcode both missing"));
    }

    // ── Test 8: "items" anahtarı altında dizi — desteklenmeli ────────────────

    [Fact]
    public async Task ParseAsync_ItemsKeyJson_ReturnsParsedProducts()
    {
        var json = """
            {
              "items": [
                { "sku": "ITM-001", "name": "Öğe 1", "price": 15.00 }
              ]
            }
            """;
        using var stream = ToStream(json);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].SKU.Should().Be("ITM-001");
    }

    // ── Test 9: Özel alan eşlemesi ────────────────────────────────────────────

    [Fact]
    public async Task ParseAsync_CustomFieldMapping_MapsCorrectly()
    {
        var json = """
            [
              {
                "stok_kodu": "OZEL-001",
                "urun_adi": "Özel JSON Ürünü",
                "liste_fiyati": 88.00
              }
            ]
            """;
        using var stream = ToStream(json);
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
        result.Products[0].Name.Should().Be("Özel JSON Ürünü");
        result.Products[0].Price.Should().Be(88.00m);
    }

    // ── Test 10: ExtraFields — bilinmeyen alanlar extra'ya gitmeli ────────────

    [Fact]
    public async Task ParseAsync_UnknownProperties_StoredInExtraFields()
    {
        var json = """
            [
              {
                "sku": "XF-001",
                "name": "Extra Ürün",
                "price": 50.00,
                "renk": "Yeşil",
                "tedarikci_kodu": "SUP-123"
              }
            ]
            """;
        using var stream = ToStream(json);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        var product = result.Products[0];
        product.ExtraFields.Should().ContainKey("renk");
        product.ExtraFields["renk"].Should().Be("Yeşil");
        product.ExtraFields.Should().ContainKey("tedarikci_kodu");
    }

    // ── Test 11: "urunler" Türkçe anahtar — desteklenmeli ────────────────────

    [Fact]
    public async Task ParseAsync_TurkishUrunlerKey_ReturnsParsedProducts()
    {
        var json = """
            {
              "urunler": [
                { "sku": "TR-001", "name": "Türkçe Anahtar Ürün", "price": 120.00 }
              ]
            }
            """;
        using var stream = ToStream(json);

        var result = await _sut.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(1);
        result.Products[0].SKU.Should().Be("TR-001");
    }

    // ── Test 12: ValidateAsync — geçerli JSON ────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ValidJson_ReturnsIsValidTrue()
    {
        var json = """
            [
              { "sku": "V-001", "price": 10.00 },
              { "sku": "V-002", "price": 20.00 }
            ]
            """;
        using var stream = ToStream(json);

        var result = await _sut.ValidateAsync(stream);

        result.IsValid.Should().BeTrue();
        result.EstimatedProductCount.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }
}
