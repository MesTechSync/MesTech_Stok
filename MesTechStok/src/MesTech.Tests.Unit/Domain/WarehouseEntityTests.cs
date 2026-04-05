using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

// ═══════════════════════════════════════════════════════
// Brand Tests
// ═══════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class BrandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsBrand()
    {
        var brand = Brand.Create(_tenantId, "Samsung", "https://logo.com/samsung.png");
        brand.Name.Should().Be("Samsung");
        brand.LogoUrl.Should().Be("https://logo.com/samsung.png");
        brand.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => Brand.Create(_tenantId, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyTenantId_Throws()
    {
        var act = () => Brand.Create(Guid.Empty, "Test");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_TrimsName()
    {
        var brand = Brand.Create(_tenantId, "  Apple  ");
        brand.Name.Should().Be("Apple");
    }

    [Fact]
    public void Rename_ValidName_Updates()
    {
        var brand = Brand.Create(_tenantId, "Old");
        brand.Rename("New Brand");
        brand.Name.Should().Be("New Brand");
    }

    [Fact]
    public void Rename_EmptyName_Throws()
    {
        var brand = Brand.Create(_tenantId, "Test");
        var act = () => brand.Rename("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_TrimsName()
    {
        var brand = Brand.Create(_tenantId, "Test");
        brand.Rename("  Trimmed  ");
        brand.Name.Should().Be("Trimmed");
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var brand = Brand.Create(_tenantId, "Test");
        brand.Deactivate();
        brand.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsActive()
    {
        var brand = Brand.Create(_tenantId, "Test");
        brand.Deactivate();
        brand.Activate();
        brand.IsActive.Should().BeTrue();
    }
}

// ═══════════════════════════════════════════════════════
// WarehouseZone Tests
// ═══════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class WarehouseZoneTests
{
    [Fact]
    public void NewZone_IsActive()
    {
        var zone = new WarehouseZone { Name = "A", Code = "Z-A" };
        zone.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var zone = new WarehouseZone { Name = "A" };
        zone.Deactivate();
        zone.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsActive()
    {
        var zone = new WarehouseZone { Name = "A" };
        zone.Deactivate();
        zone.Activate();
        zone.IsActive.Should().BeTrue();
    }
}

// ═══════════════════════════════════════════════════════
// WarehouseRack Tests
// ═══════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class WarehouseRackTests
{
    [Fact]
    public void NewRack_IsActive()
    {
        var rack = new WarehouseRack { Name = "R1", Code = "RACK-01" };
        rack.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var rack = new WarehouseRack { Name = "R1" };
        rack.Deactivate();
        rack.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsActive()
    {
        var rack = new WarehouseRack { Name = "R1" };
        rack.Deactivate();
        rack.Activate();
        rack.IsActive.Should().BeTrue();
    }
}
