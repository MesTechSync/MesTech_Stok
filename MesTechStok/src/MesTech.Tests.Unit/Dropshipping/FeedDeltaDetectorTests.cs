using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Integration.FeedParsers;
using Xunit;

namespace MesTech.Tests.Unit.Dropshipping;

/// <summary>
/// FeedDeltaDetector unit testleri — Gorev 5.5 (H27).
/// 5 temel delta senaryosu + ek testler.
/// FeedDeltaDetector: Infrastructure katmaninda, ParsedProduct ile mevcut Product karsilastirir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "FeedDeltaDetector")]
public class FeedDeltaDetectorTests
{
    private static Product MakeProduct(string sku, decimal price, int stock, bool isActive = true)
    {
        var product = new Product
        {
            SKU = sku,
            Name = $"Test Urun {sku}",
            SalePrice = price,
            IsActive = isActive,
            TenantId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid()
        };
        product.SyncStock(stock, "test-seed");
        return product;
    }

    private static ParsedProduct MakeParsed(string sku, decimal? price = null, int? quantity = null)
        => new(SKU: sku, Barcode: null, Name: $"Feed Urun {sku}", Description: null,
               Price: price, Quantity: quantity, Category: null,
               ImageUrl: null, Brand: null, Model: null,
               ExtraFields: new Dictionary<string, string>());

    // ── Test 1: Yeni urun tespiti ───────────────────────────────────

    [Fact]
    public void FeedDeltaDetector_DetectsNewProducts()
    {
        var incoming = new List<ParsedProduct>
        {
            MakeParsed("NEW-001", price: 100m, quantity: 10),
            MakeParsed("EXISTING-001", price: 200m, quantity: 5)
        };

        var existingBySku = new Dictionary<string, Product>
        {
            ["EXISTING-001"] = MakeProduct("EXISTING-001", 200m, 5)
        };

        var changes = FeedDeltaDetector.GetChangedProducts(incoming, existingBySku).ToList();

        // NEW-001 yeni urun (existing yok)
        changes.Should().ContainSingle(c => c.Incoming.SKU == "NEW-001" && c.Existing == null,
            "mevcut olmayan urun yeni kabul edilmeli");
    }

    // ── Test 2: Fiyat degisimi tespiti ──────────────────────────────

    [Fact]
    public void FeedDeltaDetector_DetectsPriceChange()
    {
        var incoming = new List<ParsedProduct>
        {
            MakeParsed("SKU-100", price: 150m, quantity: 10)  // fiyat degisti
        };

        var existingBySku = new Dictionary<string, Product>
        {
            ["SKU-100"] = MakeProduct("SKU-100", 120m, 10)   // DB'de 120 TL
        };

        var changes = FeedDeltaDetector.GetChangedProducts(incoming, existingBySku).ToList();

        changes.Should().ContainSingle(c => c.Incoming.SKU == "SKU-100",
            "fiyat degisimi olan urun degisiklik listesinde olmali");
    }

    // ── Test 3: Stok degisimi tespiti ───────────────────────────────

    [Fact]
    public void FeedDeltaDetector_DetectsStockChange()
    {
        var incoming = new List<ParsedProduct>
        {
            MakeParsed("SKU-200", price: 100m, quantity: 50)  // stok degisti (20 -> 50)
        };

        var existingBySku = new Dictionary<string, Product>
        {
            ["SKU-200"] = MakeProduct("SKU-200", 100m, 20)   // DB'de 20 adet
        };

        var changes = FeedDeltaDetector.GetChangedProducts(incoming, existingBySku).ToList();

        changes.Should().ContainSingle(c => c.Incoming.SKU == "SKU-200",
            "stok degisimi olan urun degisiklik listesinde olmali");
    }

    // ── Test 4: Kaldirilan urun (stok sifir -> deaktif) tespiti ─────

    [Fact]
    public void FeedDeltaDetector_DetectsRemovedProducts()
    {
        // Stok 0 geldi — aktif urun pasife donmeli
        var incoming = new List<ParsedProduct>
        {
            MakeParsed("SKU-300", price: 80m, quantity: 0)   // stok sifir
        };

        var existingBySku = new Dictionary<string, Product>
        {
            ["SKU-300"] = MakeProduct("SKU-300", 80m, 15, isActive: true)  // DB aktif
        };

        var changes = FeedDeltaDetector.GetChangedProducts(incoming, existingBySku).ToList();

        changes.Should().ContainSingle(c => c.Incoming.SKU == "SKU-300",
            "stok sifir olan aktif urun degisiklik olarak algilanmali (deaktif edilecek)");
    }

    // ── Test 5: Bos mevcut feed → tum urunler yeni ───────────────────

    [Fact]
    public void FeedDeltaDetector_EmptyOldFeed_AllProductsAreNew()
    {
        var incoming = new List<ParsedProduct>
        {
            MakeParsed("A001", price: 100m, quantity: 10),
            MakeParsed("A002", price: 200m, quantity: 20),
            MakeParsed("A003", price: 300m, quantity: 30),
        };

        // Mevcut DB kaydi yok
        var existingBySku = new Dictionary<string, Product>();

        var changes = FeedDeltaDetector.GetChangedProducts(incoming, existingBySku).ToList();

        changes.Should().HaveCount(3, "tum urunler yeni — 3 degisiklik olmali");
        changes.Should().OnlyContain(c => c.Existing == null,
            "tum urunler yeni oldugu icin Existing null olmali");
    }

    // ── Ek testler ──────────────────────────────────────────────────

    [Fact]
    public void FeedDeltaDetector_HasChanged_SamePriceAndStock_ReturnsFalse()
    {
        var incoming = MakeParsed("SKU-400", price: 100m, quantity: 10);
        var existing = MakeProduct("SKU-400", 100m, 10, isActive: true);

        var changed = FeedDeltaDetector.HasChanged(incoming, existing);

        changed.Should().BeFalse("fiyat ve stok degismemisse HasChanged false donmeli");
    }

    [Fact]
    public void FeedDeltaDetector_GetChangedProducts_EmptyIncoming_ReturnsEmpty()
    {
        var incoming = new List<ParsedProduct>();
        var existingBySku = new Dictionary<string, Product>
        {
            ["SKU-500"] = MakeProduct("SKU-500", 50m, 5)
        };

        var changes = FeedDeltaDetector.GetChangedProducts(incoming, existingBySku).ToList();

        changes.Should().BeEmpty("gelen feed bos ise degisiklik listesi de bos olmali");
    }

    [Fact]
    public void FeedDeltaDetector_GetChangedProducts_WithMarkupFunc_DetectsPriceChange()
    {
        var incoming = new List<ParsedProduct>
        {
            MakeParsed("SKU-600", price: 100m, quantity: 10)  // hammadde fiyati 100
        };
        var existingBySku = new Dictionary<string, Product>
        {
            ["SKU-600"] = MakeProduct("SKU-600", 130m, 10)  // DB'de 130 (yuzde 30 markup'li)
        };

        // Markup %20 uygulaniyor (100 * 1.20 = 120 != 130 → degisiklik)
        var changes = FeedDeltaDetector.GetChangedProducts(
            incoming, existingBySku,
            markupFunc: p => p * 1.20m).ToList();

        changes.Should().ContainSingle(c => c.Incoming.SKU == "SKU-600",
            "markup uygulaninca fiyat farkli cikiyor — degisiklik algilanmali");
    }

    [Fact]
    public void FeedDeltaDetector_HasChanged_PriceWithin1KurushTolerance_ReturnsFalse()
    {
        // 0.01 TL tolerans icinde — degisiklik kabul edilmemeli
        var incoming = MakeParsed("SKU-700", price: 100.005m, quantity: 10);
        var existing = MakeProduct("SKU-700", 100.00m, 10, isActive: true);

        var changed = FeedDeltaDetector.HasChanged(incoming, existing);

        changed.Should().BeFalse("0.005 TL fark 0.01 TL tolerans icinde — degisiklik olmamali");
    }
}
