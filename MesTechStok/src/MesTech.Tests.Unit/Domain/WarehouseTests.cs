using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class WarehouseTests
{
    [Fact]
    public void DisplayName_WithCode_ShouldIncludeBrackets()
    {
        var wh = new Warehouse { Code = "WH-01", Name = "Main Warehouse" };
        wh.DisplayName.Should().Be("[WH-01] Main Warehouse");
    }

    [Fact]
    public void DisplayName_WithoutCode_ShouldReturnNameOnly()
    {
        var wh = new Warehouse { Code = "", Name = "Main Warehouse" };
        wh.DisplayName.Should().Be("Main Warehouse");
    }

    [Fact]
    public void ToString_ShouldReturnDisplayName()
    {
        var wh = new Warehouse { Code = "WH-02", Name = "Secondary" };
        wh.ToString().Should().Be("[WH-02] Secondary");
    }

    [Fact]
    public void Products_ShouldBeEmptyByDefault()
    {
        var wh = new Warehouse();
        wh.Products.Should().BeEmpty();
    }

    [Fact]
    public void Defaults_ShouldBeCorrect()
    {
        var wh = new Warehouse();
        wh.IsActive.Should().BeTrue();
        wh.IsDefault.Should().BeFalse();
        wh.Type.Should().Be("MAIN");
        wh.HasClimateControl.Should().BeFalse();
    }
}
