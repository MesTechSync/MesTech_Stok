using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// Warehouse entity edge case tests — display name, default values.
/// Dalga 14 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class WarehouseEdgeCaseTests
{
    [Fact]
    public void DisplayName_WithCode_ShouldIncludeCodeInBrackets()
    {
        var warehouse = new Warehouse { Name = "Ana Depo", Code = "WH-01" };

        warehouse.DisplayName.Should().Be("[WH-01] Ana Depo");
    }

    [Fact]
    public void DisplayName_WithEmptyCode_ShouldReturnNameOnly()
    {
        var warehouse = new Warehouse { Name = "Yedek Depo", Code = "" };

        warehouse.DisplayName.Should().Be("Yedek Depo");
    }

    [Fact]
    public void DisplayName_WithWhitespaceCode_ShouldReturnNameOnly()
    {
        var warehouse = new Warehouse { Name = "Test Depo", Code = "   " };

        warehouse.DisplayName.Should().Be("Test Depo");
    }

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        var warehouse = new Warehouse { Name = "Merkez", Code = "MRK" };

        warehouse.ToString().Should().Be("[MRK] Merkez");
    }

    [Fact]
    public void DefaultType_ShouldBeMain()
    {
        var warehouse = new Warehouse();
        warehouse.Type.Should().Be("MAIN");
    }

    [Fact]
    public void DefaultIsActive_ShouldBeTrue()
    {
        var warehouse = new Warehouse();
        warehouse.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Products_ShouldBeReadOnlyCollection()
    {
        var warehouse = new Warehouse();
        warehouse.Products.Should().BeEmpty();
        warehouse.Products.Should().BeAssignableTo<IReadOnlyCollection<Product>>();
    }

    [Fact]
    public void ClimateControl_DefaultValues_ShouldBeFalse()
    {
        var warehouse = new Warehouse();
        warehouse.HasClimateControl.Should().BeFalse();
        warehouse.MinTemperature.Should().BeNull();
        warehouse.MaxTemperature.Should().BeNull();
    }

    [Fact]
    public void SecurityAndInfra_DefaultValues_ShouldBeFalse()
    {
        var warehouse = new Warehouse();
        warehouse.HasSecuritySystem.Should().BeFalse();
        warehouse.HasFireProtection.Should().BeFalse();
        warehouse.HasLoadingDock.Should().BeFalse();
        warehouse.HasRacking.Should().BeFalse();
        warehouse.HasForklift.Should().BeFalse();
    }
}
