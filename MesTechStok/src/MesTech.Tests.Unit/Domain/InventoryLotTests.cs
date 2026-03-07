using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// InventoryLot FEFO domain logic koruma testleri.
/// </summary>
[Trait("Category", "Unit")]
public class InventoryLotTests
{
    [Fact]
    public void Consume_ShouldDecreaseRemainingQty()
    {
        var lot = FakeData.CreateLot(Guid.NewGuid(), 100, 50);

        lot.Consume(20);

        lot.RemainingQty.Should().Be(30);
        lot.Status.Should().Be(LotStatus.Open);
    }

    [Fact]
    public void Consume_AllRemaining_ShouldCloseLot()
    {
        var lot = FakeData.CreateLot(Guid.NewGuid(), 100, 30);

        lot.Consume(30);

        lot.RemainingQty.Should().Be(0);
        lot.Status.Should().Be(LotStatus.Closed);
        lot.ClosedDate.Should().NotBeNull();
    }

    [Fact]
    public void Consume_MoreThanRemaining_ShouldThrow()
    {
        var lot = FakeData.CreateLot(Guid.NewGuid(), 100, 20);

        var act = () => lot.Consume(50);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsExpired_WhenPastExpiryDate_ShouldReturnTrue()
    {
        var lot = new InventoryLot
        {
            ExpiryDate = DateTime.UtcNow.AddDays(-1),
            RemainingQty = 50,
            Status = LotStatus.Open
        };

        lot.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenFutureExpiryDate_ShouldReturnFalse()
    {
        var lot = FakeData.CreateLot(Guid.NewGuid(), expiryDate: DateTime.UtcNow.AddMonths(6));

        lot.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenNoExpiryDate_ShouldReturnFalse()
    {
        var lot = new InventoryLot
        {
            ExpiryDate = null,
            RemainingQty = 50,
            Status = LotStatus.Open
        };

        lot.IsExpired.Should().BeFalse();
    }
}
