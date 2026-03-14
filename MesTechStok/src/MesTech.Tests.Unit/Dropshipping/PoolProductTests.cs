using FluentAssertions;
using MesTech.Domain.Entities;
using Xunit;

namespace MesTech.Tests.Unit.Dropshipping;

/// <summary>
/// DropshippingPoolProduct entity unit testleri — Gorev 5.3 (H27).
/// 6 test: Create, UpdatePrice, Activate, Deactivate, empty-guard, negative-price-guard.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "DropshippingPoolProduct")]
public class PoolProductTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _poolId = Guid.NewGuid();
    private static readonly Guid _productId = Guid.NewGuid();

    /// <summary>Test 1: Create — dogru propertyleri set eder.</summary>
    [Fact]
    public void PoolProduct_Create_SetsCorrectProperties()
    {
        var feedId = Guid.NewGuid();
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 299.90m, feedId);

        pp.TenantId.Should().Be(_tenantId);
        pp.PoolId.Should().Be(_poolId);
        pp.ProductId.Should().Be(_productId);
        pp.PoolPrice.Should().Be(299.90m);
        pp.AddedFromFeedId.Should().Be(feedId);
        pp.IsActive.Should().BeTrue("yeni havuz urunu varsayilan olarak aktif olmali");
    }

    /// <summary>Test 2: Havuza ekle — feedId olmadan manuel ekleme.</summary>
    [Fact]
    public void PoolProduct_AddToStore_ManuallyWithoutFeedId_IsAllowed()
    {
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 150m, addedFromFeedId: null);

        pp.AddedFromFeedId.Should().BeNull("manuel eklenen urunlerde feedId null olmali");
        pp.IsActive.Should().BeTrue();
    }

    /// <summary>Test 3: Havuzdan cikar (Deactivate).</summary>
    [Fact]
    public void PoolProduct_RemoveFromStore_SetsIsActiveFalse()
    {
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        pp.Deactivate();

        pp.IsActive.Should().BeFalse("Deactivate sonrasi IsActive false olmali");
    }

    /// <summary>Test 4: Marjin guncelle.</summary>
    [Fact]
    public void PoolProduct_UpdateMargin_UpdatesPoolPrice()
    {
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        pp.UpdatePrice(175m);

        pp.PoolPrice.Should().Be(175m);
    }

    /// <summary>Test 5: Onay — Activate calistirir (tekrar aktif).</summary>
    [Fact]
    public void PoolProduct_Approve_ActivatesProduct()
    {
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        pp.Deactivate();
        pp.IsActive.Should().BeFalse();

        pp.Activate();

        pp.IsActive.Should().BeTrue("Activate sonrasi urun onaylandi — IsActive true olmali");
    }

    /// <summary>Test 6: Red — Deactivate + negatif fiyat guard.</summary>
    [Fact]
    public void PoolProduct_Reject_NegativePrice_ThrowsArgumentOutOfRange()
    {
        var act = () => new DropshippingPoolProduct(_tenantId, _poolId, _productId, -0.01m);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("poolPrice",
                "negatif fiyat ile havuz urunu olusturulamaz");
    }

    // ── Ek testler ──────────────────────────────────────────────────

    [Fact]
    public void PoolProduct_EmptyPoolId_ThrowsArgumentException()
    {
        var act = () => new DropshippingPoolProduct(_tenantId, Guid.Empty, _productId, 100m);

        act.Should().Throw<ArgumentException>().WithParameterName("poolId");
    }

    [Fact]
    public void PoolProduct_EmptyProductId_ThrowsArgumentException()
    {
        var act = () => new DropshippingPoolProduct(_tenantId, _poolId, Guid.Empty, 100m);

        act.Should().Throw<ArgumentException>().WithParameterName("productId");
    }

    [Fact]
    public void PoolProduct_UpdatePrice_ToNegative_ThrowsArgumentOutOfRange()
    {
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 100m);
        var act = () => pp.UpdatePrice(-50m);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("newPrice");
    }

    [Fact]
    public void PoolProduct_ZeroPrice_IsAllowed()
    {
        var pp = new DropshippingPoolProduct(_tenantId, _poolId, _productId, 0m);

        pp.PoolPrice.Should().Be(0m, "sifir fiyat gecerli — henuz fiyatlandirilmamis urun");
    }
}
