using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// InventoryLot edge case tests — consumption, expiry, boundary values.
/// Dalga 14 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class InventoryLotEdgeCaseTests
{
    [Fact]
    public void Consume_ExactRemaining_ShouldCloseLot()
    {
        var lot = FakeData.CreateLot(Guid.NewGuid(), remainingQty: 50);

        lot.Consume(50);

        lot.RemainingQty.Should().Be(0);
        lot.Status.Should().Be(LotStatus.Closed);
        lot.ClosedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Consume_PartialAmount_ShouldRemainOpen()
    {
        var lot = FakeData.CreateLot(Guid.NewGuid(), remainingQty: 100);

        lot.Consume(30);

        lot.RemainingQty.Should().Be(70);
        lot.Status.Should().Be(LotStatus.Open);
        lot.ClosedDate.Should().BeNull();
    }

    [Fact]
    public void Consume_MoreThanRemaining_ShouldThrow()
    {
        var lot = FakeData.CreateLot(Guid.NewGuid(), remainingQty: 10);

        var act = () => lot.Consume(20);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot consume*");
    }

    [Fact]
    public void Consume_ZeroQuantity_ShouldNotChangeLot()
    {
        var lot = FakeData.CreateLot(Guid.NewGuid(), remainingQty: 50);

        lot.Consume(0);

        lot.RemainingQty.Should().Be(50);
        lot.Status.Should().Be(LotStatus.Open);
    }

    [Fact]
    public void IsExpired_WhenPastExpiryDate_ShouldReturnTrue()
    {
        var lot = FakeData.CreateLot(Guid.NewGuid(),
            expiryDate: DateTime.UtcNow.AddDays(-1));

        lot.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenFutureExpiryDate_ShouldReturnFalse()
    {
        var lot = FakeData.CreateLot(Guid.NewGuid(),
            expiryDate: DateTime.UtcNow.AddMonths(6));

        lot.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenNoExpiryDate_ShouldReturnFalse()
    {
        var lot = new InventoryLot
        {
            ProductId = Guid.NewGuid(),
            LotNumber = "LOT-NO-EXP",
            ExpiryDate = null,
            RemainingQty = 50,
            Status = LotStatus.Open
        };

        lot.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void DefaultStatus_ShouldBeOpen()
    {
        var lot = new InventoryLot();
        lot.Status.Should().Be(LotStatus.Open);
    }
}
