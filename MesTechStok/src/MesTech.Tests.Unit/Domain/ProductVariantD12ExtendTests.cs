using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// D12-17 — ProductVariant D12-02 genişletme testleri.
/// SetDimensions, SetCompareAtPrice, AddImageUrl, SetSortOrder, VariantBarcode.
/// Mevcut ProductVariantTests'e DOKUNMAZ.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "ProductVariant")]
public class ProductVariantD12ExtendTests
{
    private static ProductVariant MakeVariant() =>
        ProductVariant.Create(Guid.NewGuid(), "VAR-D12-001", stock: 10, price: 99.90m);

    // ══════════════════════════════════════
    // SetDimensions
    // ══════════════════════════════════════

    [Fact]
    public void SetDimensions_ValidValues_ShouldSetAll()
    {
        var v = MakeVariant();
        v.SetDimensions(500m, 30m, 20m, 15m);

        v.WeightGrams.Should().Be(500m);
        v.WidthCm.Should().Be(30m);
        v.HeightCm.Should().Be(20m);
        v.DepthCm.Should().Be(15m);
    }

    [Fact]
    public void SetDimensions_NullValues_ShouldClear()
    {
        var v = MakeVariant();
        v.SetDimensions(500m, 30m, 20m, 15m);
        v.SetDimensions(null, null, null, null);

        v.WeightGrams.Should().BeNull();
        v.WidthCm.Should().BeNull();
    }

    [Fact]
    public void SetDimensions_PartialNull_ShouldWork()
    {
        var v = MakeVariant();
        v.SetDimensions(250m, null, 10m, null);

        v.WeightGrams.Should().Be(250m);
        v.WidthCm.Should().BeNull();
        v.HeightCm.Should().Be(10m);
        v.DepthCm.Should().BeNull();
    }

    // ══════════════════════════════════════
    // SetCompareAtPrice
    // ══════════════════════════════════════

    [Fact]
    public void SetCompareAtPrice_ValidPrice_ShouldSet()
    {
        var v = MakeVariant();
        v.SetCompareAtPrice(149.90m);

        v.CompareAtPrice.Should().Be(149.90m);
    }

    [Fact]
    public void SetCompareAtPrice_Null_ShouldClear()
    {
        var v = MakeVariant();
        v.SetCompareAtPrice(149.90m);
        v.SetCompareAtPrice(null);

        v.CompareAtPrice.Should().BeNull();
    }

    [Fact]
    public void SetCompareAtPrice_Negative_ShouldThrow()
    {
        var v = MakeVariant();
        var act = () => v.SetCompareAtPrice(-10m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetCompareAtPrice_Zero_ShouldSucceed()
    {
        var v = MakeVariant();
        v.SetCompareAtPrice(0m);

        v.CompareAtPrice.Should().Be(0m);
    }

    // ══════════════════════════════════════
    // AddImageUrl + GetImageUrls
    // ══════════════════════════════════════

    [Fact]
    public void AddImageUrl_SingleUrl_ShouldAdd()
    {
        var v = MakeVariant();
        v.AddImageUrl("https://cdn.example.com/img1.jpg");

        v.GetImageUrls().Should().HaveCount(1);
        v.GetImageUrls()[0].Should().Be("https://cdn.example.com/img1.jpg");
    }

    [Fact]
    public void AddImageUrl_Duplicate_ShouldNotAdd()
    {
        var v = MakeVariant();
        v.AddImageUrl("https://cdn.example.com/img1.jpg");
        v.AddImageUrl("https://cdn.example.com/img1.jpg");

        v.GetImageUrls().Should().HaveCount(1, "duplicate URL should be ignored");
    }

    [Fact]
    public void AddImageUrl_MultipleUnique_ShouldAddAll()
    {
        var v = MakeVariant();
        v.AddImageUrl("https://cdn.example.com/img1.jpg");
        v.AddImageUrl("https://cdn.example.com/img2.jpg");
        v.AddImageUrl("https://cdn.example.com/img3.jpg");

        v.GetImageUrls().Should().HaveCount(3);
    }

    [Fact]
    public void AddImageUrl_Empty_ShouldThrow()
    {
        var v = MakeVariant();
        var act = () => v.AddImageUrl("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetImageUrls_NoImages_ShouldReturnEmpty()
    {
        var v = MakeVariant();
        v.GetImageUrls().Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // SetSortOrder
    // ══════════════════════════════════════

    [Fact]
    public void SetSortOrder_ShouldSet()
    {
        var v = MakeVariant();
        v.SetSortOrder(5);

        v.SortOrder.Should().Be(5);
    }

    // ══════════════════════════════════════
    // VariantBarcode
    // ══════════════════════════════════════

    [Fact]
    public void VariantBarcode_ShouldBeSettable()
    {
        var v = MakeVariant();
        v.VariantBarcode = "8690001000011";

        v.VariantBarcode.Should().Be("8690001000011");
    }

    [Fact]
    public void VariantBarcode_Null_ShouldBeAllowed()
    {
        var v = MakeVariant();
        v.VariantBarcode = null;

        v.VariantBarcode.Should().BeNull();
    }
}
