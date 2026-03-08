using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class UnitOfMeasureTests
{
    [Fact]
    public void Piece_Default_ShouldCreatePCS()
    {
        var uom = UnitOfMeasure.Piece();
        uom.Unit.Should().Be("PCS");
        uom.Quantity.Should().Be(1);
    }

    [Fact]
    public void Kilogram_WithQuantity_ShouldCreateKG()
    {
        var uom = UnitOfMeasure.Kilogram(2.5m);
        uom.Unit.Should().Be("KG");
        uom.Quantity.Should().Be(2.5m);
    }

    [Fact]
    public void Gram_ShouldCreateGR()
    {
        var uom = UnitOfMeasure.Gram(500);
        uom.Unit.Should().Be("GR");
        uom.Quantity.Should().Be(500);
    }

    [Fact]
    public void Liter_ShouldCreateLT()
    {
        var uom = UnitOfMeasure.Liter(10);
        uom.Unit.Should().Be("LT");
    }

    [Fact]
    public void Meter_ShouldCreateMT()
    {
        var uom = UnitOfMeasure.Meter(3);
        uom.Unit.Should().Be("MT");
    }

    [Fact]
    public void Box_ShouldCreateBOX()
    {
        var uom = UnitOfMeasure.Box(12);
        uom.Unit.Should().Be("BOX");
        uom.Quantity.Should().Be(12);
    }

    [Fact]
    public void Pallet_ShouldCreatePLT()
    {
        var uom = UnitOfMeasure.Pallet(4);
        uom.Unit.Should().Be("PLT");
        uom.Quantity.Should().Be(4);
    }

    [Fact]
    public void ToString_ShouldReturnQuantityAndUnit()
    {
        var uom = UnitOfMeasure.Kilogram(5);
        uom.ToString().Should().Contain("5").And.Contain("KG");
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var a = UnitOfMeasure.Piece(10);
        var b = new UnitOfMeasure("PCS", 10);
        a.Should().Be(b);
    }
}
