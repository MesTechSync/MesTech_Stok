using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// D12-19 — PlatformAttributeValueMapping entity testleri.
/// İç değer ↔ platform ID eşleştirmesi.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "D12")]
public class PlatformAttributeValueMappingD12Tests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public void Create_ValidTrendyol_ShouldSucceed()
    {
        var m = PlatformAttributeValueMapping.Create(
            TenantId, "Renk", "Kırmızı", PlatformType.Trendyol, 338, 6980, "Kirmizi");

        m.InternalAttributeName.Should().Be("Renk");
        m.InternalValue.Should().Be("Kırmızı");
        m.PlatformType.Should().Be(PlatformType.Trendyol);
        m.PlatformAttributeId.Should().Be(338);
        m.PlatformValueId.Should().Be(6980);
        m.PlatformValueName.Should().Be("Kirmizi");
    }

    [Fact]
    public void Create_WithSlicerAndVarianter()
    {
        var m = PlatformAttributeValueMapping.Create(
            TenantId, "Beden", "M", PlatformType.Hepsiburada, 100, 200,
            isSlicer: true, isVarianter: true);

        m.IsSlicer.Should().BeTrue();
        m.IsVarianter.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyTenantId_ShouldThrow()
    {
        var act = () => PlatformAttributeValueMapping.Create(
            Guid.Empty, "Renk", "Kırmızı", PlatformType.Trendyol, 338, 6980);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyAttributeName_ShouldThrow()
    {
        var act = () => PlatformAttributeValueMapping.Create(
            TenantId, "", "Kırmızı", PlatformType.Trendyol, 338, 6980);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyValue_ShouldThrow()
    {
        var act = () => PlatformAttributeValueMapping.Create(
            TenantId, "Renk", "", PlatformType.Trendyol, 338, 6980);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetAutoMapped_ShouldSetFlag()
    {
        var m = PlatformAttributeValueMapping.Create(
            TenantId, "Renk", "Mavi", PlatformType.N11, 50, 100);
        m.SetAutoMapped(true);

        m.IsAutoMapped.Should().BeTrue();
    }

    [Fact]
    public void LockAfterApproval_ShouldPreventUpdate()
    {
        var m = PlatformAttributeValueMapping.Create(
            TenantId, "Renk", "Yeşil", PlatformType.Trendyol, 338, 7000);
        m.LockAfterApproval();

        m.IsLockedAfterApproval.Should().BeTrue();

        var act = () => m.UpdatePlatformIds(999, 888, "Yeni");
        act.Should().Throw<InvalidOperationException>().WithMessage("*Onay sonrasi*");
    }

    [Fact]
    public void UpdatePlatformIds_NotLocked_ShouldUpdate()
    {
        var m = PlatformAttributeValueMapping.Create(
            TenantId, "Beden", "L", PlatformType.Trendyol, 47, 200);
        m.UpdatePlatformIds(48, 201, "Large");

        m.PlatformAttributeId.Should().Be(48);
        m.PlatformValueId.Should().Be(201);
        m.PlatformValueName.Should().Be("Large");
    }

    [Fact]
    public void UpdatePlatformIds_NullValues_ShouldClear()
    {
        var m = PlatformAttributeValueMapping.Create(
            TenantId, "Renk", "Siyah", PlatformType.Trendyol, 338, 5000, "Black");
        m.UpdatePlatformIds(null, null, null);

        m.PlatformAttributeId.Should().BeNull();
        m.PlatformValueId.Should().BeNull();
        m.PlatformValueName.Should().BeNull();
    }

    [Theory]
    [InlineData(PlatformType.Trendyol)]
    [InlineData(PlatformType.Hepsiburada)]
    [InlineData(PlatformType.N11)]
    [InlineData(PlatformType.Amazon)]
    public void Create_AllPlatforms_ShouldWork(PlatformType platform)
    {
        var m = PlatformAttributeValueMapping.Create(
            TenantId, "Material", "Cotton", platform, 1, 1);
        m.PlatformType.Should().Be(platform);
    }
}
