using FluentAssertions;
using MesTech.Application.Helpers;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// I-06 Gorev 2: FIFO maliyet hesaplama dogruluk testleri.
/// </summary>
public class FifoCalculatorTests
{
    [Fact]
    public void FifoCost_ShouldUseOldestLotFirst()
    {
        // Arrange: 3 lot, farkli tarih ve birim maliyet
        var lots = new List<FifoLotInput>
        {
            new("L001", new DateTime(2026, 1, 15), 20, 85m),
            new("L002", new DateTime(2026, 2, 1), 35, 92m),
            new("L003", new DateTime(2026, 3, 10), 50, 88.5m),
        };

        // Act: 30 adet satis — 20x85 + 10x92 = 1700 + 920 = 2620
        var cost = FifoCalculator.CalculateFifoCost(lots, 30);

        // Assert
        cost.Should().Be(2620m);
    }

    [Fact]
    public void FifoCost_EntireLotConsumed_ShouldMoveToNext()
    {
        // Arrange
        var lots = new List<FifoLotInput>
        {
            new("L001", new DateTime(2026, 1, 1), 10, 100m),
            new("L002", new DateTime(2026, 2, 1), 10, 120m),
        };

        // Act: 15 adet — 10x100 + 5x120 = 1000 + 600 = 1600
        var cost = FifoCalculator.CalculateFifoCost(lots, 15);

        // Assert
        cost.Should().Be(1600m);
    }

    [Fact]
    public void FifoCost_InsufficientStock_ShouldCalculateAvailable()
    {
        // Arrange: sadece 5 adet mevcut
        var lots = new List<FifoLotInput>
        {
            new("L001", DateTime.Now, 5, 100m),
        };

        // Act: 10 adet istendi ama 5 var — sadece mevcut kadar hesapla
        var cost = FifoCalculator.CalculateFifoCost(lots, 10);

        // Assert
        cost.Should().Be(500m);
    }

    [Fact]
    public void FifoCost_EmptyLots_ShouldReturnZero()
    {
        var cost = FifoCalculator.CalculateFifoCost(Array.Empty<FifoLotInput>(), 10);
        cost.Should().Be(0m);
    }

    [Fact]
    public void FifoCost_ZeroQuantity_ShouldReturnZero()
    {
        var lots = new List<FifoLotInput>
        {
            new("L001", DateTime.Now, 50, 100m),
        };

        var cost = FifoCalculator.CalculateFifoCost(lots, 0);
        cost.Should().Be(0m);
    }

    [Fact]
    public void ConsumptionPlan_ShouldReturnCorrectBreakdown()
    {
        var lots = new List<FifoLotInput>
        {
            new("L001", new DateTime(2026, 1, 15), 20, 85m),
            new("L002", new DateTime(2026, 2, 1), 35, 92m),
            new("L003", new DateTime(2026, 3, 10), 50, 88.5m),
        };

        var plan = FifoCalculator.GetConsumptionPlan(lots, 30);

        plan.Should().HaveCount(2);
        plan[0].LotNumber.Should().Be("L001");
        plan[0].ConsumedQuantity.Should().Be(20);
        plan[0].TotalCost.Should().Be(1700m);
        plan[1].LotNumber.Should().Be("L002");
        plan[1].ConsumedQuantity.Should().Be(10);
        plan[1].TotalCost.Should().Be(920m);
    }

    [Fact]
    public void FifoCost_UnsortedLots_ShouldSortByEntryDate()
    {
        // Lots given in reverse order — FIFO should still use oldest first
        var lots = new List<FifoLotInput>
        {
            new("L003", new DateTime(2026, 3, 10), 50, 88.5m),
            new("L001", new DateTime(2026, 1, 15), 20, 85m),
            new("L002", new DateTime(2026, 2, 1), 35, 92m),
        };

        var cost = FifoCalculator.CalculateFifoCost(lots, 30);
        cost.Should().Be(2620m); // Same as sorted test
    }
}
