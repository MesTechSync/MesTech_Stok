using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.Domain;

public class StockLevelTests
{
    // StockLevel(Current, Minimum, Maximum, ReorderLevel, ReorderQuantity)

    [Fact]
    public void Create_WithValidValues_ShouldSucceed()
    {
        var level = new StockLevel(50, 5, 1000, 10, 50);

        level.Current.Should().Be(50);
        level.Minimum.Should().Be(5);
        level.Maximum.Should().Be(1000);
        level.ReorderLevel.Should().Be(10);
        level.ReorderQuantity.Should().Be(50);
    }

    [Fact]
    public void Status_WhenOutOfStock_ShouldReturnOutOfStock()
    {
        var level = new StockLevel(0, 5, 1000, 10, 50);

        level.Status.Should().Be("OutOfStock");
        level.IsOutOfStock.Should().BeTrue();
    }

    [Fact]
    public void Status_WhenCritical_ShouldReturnCritical()
    {
        var level = new StockLevel(3, 10, 1000, 20, 50);

        level.Status.Should().Be("Critical");
        level.IsCritical.Should().BeTrue();
    }

    [Fact]
    public void Status_WhenLow_ShouldReturnLow()
    {
        // Current=8, Minimum=10 (IsLow=true), ReorderLevel=14, 14/2=7 (IsCritical=false since 8>7)
        var level = new StockLevel(8, 10, 1000, 14, 50);

        level.Status.Should().Be("Low");
        level.IsLow.Should().BeTrue();
    }

    [Fact]
    public void Status_WhenNormal_ShouldReturnNormal()
    {
        var level = new StockLevel(50, 5, 1000, 10, 50);

        level.Status.Should().Be("Normal");
    }

    [Fact]
    public void Status_WhenOverStock_ShouldReturnOverStock()
    {
        var level = new StockLevel(1500, 5, 1000, 10, 50);

        level.Status.Should().Be("OverStock");
        level.IsOverStock.Should().BeTrue();
    }

    [Fact]
    public void NeedsReorder_WhenBelowReorderLevel_ShouldBeTrue()
    {
        var level = new StockLevel(8, 5, 1000, 10, 50);

        level.NeedsReorder.Should().BeTrue();
        level.ReorderAmount.Should().Be(50);
    }

    [Fact]
    public void NeedsReorder_WhenAboveReorderLevel_ShouldBeFalse()
    {
        var level = new StockLevel(50, 5, 1000, 10, 50);

        level.NeedsReorder.Should().BeFalse();
        level.ReorderAmount.Should().Be(0);
    }
}
