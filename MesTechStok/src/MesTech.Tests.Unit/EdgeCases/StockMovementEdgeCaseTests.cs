using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// StockMovement edge case tests — computed properties, movement type, boundary values.
/// Dalga 14 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class StockMovementEdgeCaseTests
{
    [Fact]
    public void IsPositiveMovement_WhenQuantityPositive_ShouldReturnTrue()
    {
        var movement = new StockMovement { Quantity = 10 };

        movement.IsPositiveMovement.Should().BeTrue();
        movement.IsNegativeMovement.Should().BeFalse();
    }

    [Fact]
    public void IsNegativeMovement_WhenQuantityNegative_ShouldReturnTrue()
    {
        var movement = new StockMovement { Quantity = -5 };

        movement.IsNegativeMovement.Should().BeTrue();
        movement.IsPositiveMovement.Should().BeFalse();
    }

    [Fact]
    public void IsPositiveMovement_WhenQuantityZero_ShouldReturnFalse()
    {
        var movement = new StockMovement { Quantity = 0 };

        movement.IsPositiveMovement.Should().BeFalse();
        movement.IsNegativeMovement.Should().BeFalse();
    }

    [Fact]
    public void SetMovementType_ShouldConvertEnumToString()
    {
        var movement = new StockMovement();

        movement.SetMovementType(StockMovementType.Purchase);

        movement.MovementType.Should().Be("Purchase");
    }

    [Fact]
    public void SetMovementType_Transfer_ShouldSetCorrectly()
    {
        var movement = new StockMovement();

        movement.SetMovementType(StockMovementType.Transfer);

        movement.MovementType.Should().Be("Transfer");
    }

    [Fact]
    public void SetMovementType_PlatformSync_ShouldSetCorrectly()
    {
        var movement = new StockMovement();

        movement.SetMovementType(StockMovementType.TrendyolSync);

        movement.MovementType.Should().Be("TrendyolSync");
    }

    [Fact]
    public void ToString_ShouldContainMovementTypeAndQuantity()
    {
        var productId = Guid.NewGuid();
        var movement = new StockMovement
        {
            MovementType = "StockIn",
            Quantity = 25,
            ProductId = productId
        };

        var str = movement.ToString();

        str.Should().Contain("StockIn");
        str.Should().Contain("25");
    }

    [Fact]
    public void DefaultMovementType_ShouldBeEmpty()
    {
        var movement = new StockMovement();
        movement.MovementType.Should().BeEmpty();
    }

    [Fact]
    public void DefaultDate_ShouldBeCloseToUtcNow()
    {
        var movement = new StockMovement();
        movement.Date.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ReversalFields_DefaultState_ShouldNotBeReversed()
    {
        var movement = new StockMovement();
        movement.IsReversed.Should().BeFalse();
        movement.ReversalMovementId.Should().BeNull();
    }

    [Fact]
    public void ApprovalFields_DefaultState_ShouldNotBeApproved()
    {
        var movement = new StockMovement();
        movement.IsApproved.Should().BeFalse();
        movement.ApprovedBy.Should().BeNull();
        movement.ApprovedDate.Should().BeNull();
    }
}
