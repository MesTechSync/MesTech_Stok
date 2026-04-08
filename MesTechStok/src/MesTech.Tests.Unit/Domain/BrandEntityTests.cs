using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Brand entity creation, validation, rename, activate/deactivate tests.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Brand")]
public class BrandEntityTests
{
    private static readonly Guid ValidTenantId = Guid.NewGuid();

    // ── Create ──────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ShouldReturnBrand()
    {
        var brand = Brand.Create(ValidTenantId, "Samsung");

        brand.Should().NotBeNull();
        brand.TenantId.Should().Be(ValidTenantId);
        brand.Name.Should().Be("Samsung");
        brand.IsActive.Should().BeTrue();
        brand.LogoUrl.Should().BeNull();
        brand.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithLogoUrl_ShouldSetLogoUrl()
    {
        var brand = Brand.Create(ValidTenantId, "Apple", "https://cdn.example.com/apple.png");

        brand.LogoUrl.Should().Be("https://cdn.example.com/apple.png");
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var brand = Brand.Create(ValidTenantId, "  Samsung  ");

        brand.Name.Should().Be("Samsung");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceName_ShouldThrow(string? invalidName)
    {
        var act = () => Brand.Create(ValidTenantId, invalidName!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyTenantId_ShouldThrow()
    {
        var act = () => Brand.Create(Guid.Empty, "Samsung");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*TenantId*");
    }

    // ── Collections ─────────────────────────────────────────

    [Fact]
    public void Create_ShouldInitializeEmptyCollections()
    {
        var brand = Brand.Create(ValidTenantId, "LG");

        brand.Products.Should().NotBeNull().And.BeEmpty();
        brand.PlatformMappings.Should().NotBeNull().And.BeEmpty();
    }

    // ── Rename ──────────────────────────────────────────────

    [Fact]
    public void Rename_WithValidName_ShouldUpdateName()
    {
        var brand = Brand.Create(ValidTenantId, "OldName");

        brand.Rename("NewName");

        brand.Name.Should().Be("NewName");
    }

    [Fact]
    public void Rename_ShouldTrimNewName()
    {
        var brand = Brand.Create(ValidTenantId, "Original");

        brand.Rename("  Trimmed  ");

        brand.Name.Should().Be("Trimmed");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithNullOrWhitespaceName_ShouldThrow(string? invalidName)
    {
        var brand = Brand.Create(ValidTenantId, "Existing");

        var act = () => brand.Rename(invalidName!);

        act.Should().Throw<ArgumentException>();
    }

    // ── Activate / Deactivate ───────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var brand = Brand.Create(ValidTenantId, "Sony");

        brand.Deactivate();

        brand.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_ShouldSetIsActiveTrue()
    {
        var brand = Brand.Create(ValidTenantId, "Sony");
        brand.Deactivate();

        brand.Activate();

        brand.IsActive.Should().BeTrue();
    }

    // ── BaseEntity / ITenantEntity ──────────────────────────

    [Fact]
    public void Brand_ShouldImplementITenantEntity()
    {
        var brand = Brand.Create(ValidTenantId, "Bosch");

        brand.Should().BeAssignableTo<MesTech.Domain.Common.ITenantEntity>();
        brand.TenantId.Should().Be(ValidTenantId);
    }

    [Fact]
    public void Brand_ShouldInheritBaseEntity()
    {
        var brand = Brand.Create(ValidTenantId, "Philips");

        brand.Should().BeAssignableTo<MesTech.Domain.Common.BaseEntity>();
        brand.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
