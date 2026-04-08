using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// D12-18 — ProductMedia + ProductSpecification entity testleri.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "D12")]
public class ProductMediaAndSpecD12Tests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductId = Guid.NewGuid();

    // ══════════════════════════════════════
    // ProductMedia — Create
    // ══════════════════════════════════════

    [Fact]
    public void Media_Create_ValidImage_ShouldSucceed()
    {
        var m = ProductMedia.Create(TenantId, ProductId, MediaType.Image,
            "https://cdn.example.com/img.jpg", 0);

        m.Type.Should().Be(MediaType.Image);
        m.Url.Should().Be("https://cdn.example.com/img.jpg");
        m.SortOrder.Should().Be(0);
        m.ProductId.Should().Be(ProductId);
        m.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void Media_Create_Video_WithAltText()
    {
        var m = ProductMedia.Create(TenantId, ProductId, MediaType.Video,
            "https://cdn.example.com/video.mp4", 1, altText: "Ürün tanıtım videosu");

        m.Type.Should().Be(MediaType.Video);
        m.AltText.Should().Be("Ürün tanıtım videosu");
    }

    [Fact]
    public void Media_Create_WithVariantId()
    {
        var variantId = Guid.NewGuid();
        var m = ProductMedia.Create(TenantId, ProductId, MediaType.Image,
            "https://cdn.example.com/variant.jpg", 0, variantId: variantId);

        m.VariantId.Should().Be(variantId);
    }

    [Fact]
    public void Media_Create_EmptyUrl_ShouldThrow()
    {
        var act = () => ProductMedia.Create(TenantId, ProductId, MediaType.Image, "", 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Media_Create_EmptyTenantId_ShouldThrow()
    {
        var act = () => ProductMedia.Create(Guid.Empty, ProductId, MediaType.Image,
            "https://cdn.example.com/img.jpg", 0);
        act.Should().Throw<ArgumentException>();
    }

    // ══════════════════════════════════════
    // ProductMedia — Methods
    // ══════════════════════════════════════

    [Fact]
    public void Media_SetThumbnail_ShouldSet()
    {
        var m = ProductMedia.Create(TenantId, ProductId, MediaType.Image,
            "https://cdn.example.com/img.jpg", 0);
        m.SetThumbnail("https://cdn.example.com/thumb.jpg");

        m.ThumbnailUrl.Should().Be("https://cdn.example.com/thumb.jpg");
    }

    [Fact]
    public void Media_SetThumbnail_Empty_ShouldThrow()
    {
        var m = ProductMedia.Create(TenantId, ProductId, MediaType.Image,
            "https://cdn.example.com/img.jpg", 0);

        var act = () => m.SetThumbnail("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Media_SetDuration_Positive_ShouldSet()
    {
        var m = ProductMedia.Create(TenantId, ProductId, MediaType.Video,
            "https://cdn.example.com/video.mp4", 0);
        m.SetDuration(120);

        m.DurationSeconds.Should().Be(120);
    }

    [Fact]
    public void Media_SetDuration_Zero_ShouldThrow()
    {
        var m = ProductMedia.Create(TenantId, ProductId, MediaType.Video,
            "https://cdn.example.com/video.mp4", 0);

        var act = () => m.SetDuration(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Media_UpdateSortOrder_ShouldSet()
    {
        var m = ProductMedia.Create(TenantId, ProductId, MediaType.Image,
            "https://cdn.example.com/img.jpg", 0);
        m.UpdateSortOrder(5);

        m.SortOrder.Should().Be(5);
    }

    // ══════════════════════════════════════
    // MediaType enum coverage
    // ══════════════════════════════════════

    [Theory]
    [InlineData(MediaType.Image)]
    [InlineData(MediaType.Video)]
    [InlineData(MediaType.SizeChart)]
    [InlineData(MediaType.Certificate)]
    [InlineData(MediaType.PackageImage)]
    public void Media_AllTypes_ShouldCreate(MediaType type)
    {
        var m = ProductMedia.Create(TenantId, ProductId, type, "https://cdn.example.com/x", 0);
        m.Type.Should().Be(type);
    }

    // ══════════════════════════════════════
    // ProductSpecification — Create
    // ══════════════════════════════════════

    [Fact]
    public void Spec_Create_Valid_ShouldSucceed()
    {
        var s = ProductSpecification.Create(TenantId, ProductId,
            "Teknik", "Ekran Boyutu", "6.7 inç", "inç", 1);

        s.SpecGroup.Should().Be("Teknik");
        s.SpecName.Should().Be("Ekran Boyutu");
        s.SpecValue.Should().Be("6.7 inç");
        s.Unit.Should().Be("inç");
        s.DisplayOrder.Should().Be(1);
    }

    [Fact]
    public void Spec_Create_EmptyName_ShouldThrow()
    {
        var act = () => ProductSpecification.Create(TenantId, ProductId,
            "Teknik", "", "value");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Spec_Create_EmptyTenantId_ShouldThrow()
    {
        var act = () => ProductSpecification.Create(Guid.Empty, ProductId,
            "Teknik", "Renk", "Siyah");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Spec_UpdateValue_ShouldUpdate()
    {
        var s = ProductSpecification.Create(TenantId, ProductId,
            "Renk", "Ana Renk", "Kırmızı");
        s.UpdateValue("Mavi", "hex");

        s.SpecValue.Should().Be("Mavi");
        s.Unit.Should().Be("hex");
    }

    [Fact]
    public void Spec_UpdateValue_NullUnit_ShouldKeepOld()
    {
        var s = ProductSpecification.Create(TenantId, ProductId,
            "Boyut", "Genişlik", "30", "cm");
        s.UpdateValue("35");

        s.SpecValue.Should().Be("35");
        s.Unit.Should().Be("cm", "null unit should keep previous value");
    }
}
