using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;
using MesTech.Domain.Services;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// StockCalculationService edge cases — WAC boundaries, FEFO lot selection, validation.
/// Dalga 14 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class StockCalculationServiceEdgeCaseTests
{
    private readonly StockCalculationService _sut = new();

    // ── CalculateWAC Edge Cases ──

    [Fact]
    public void CalculateWAC_ZeroCurrentStock_ShouldReturnNewUnitCost()
    {
        var result = _sut.CalculateWAC(0, 0m, 100, 10m);

        result.Should().Be(10m);
    }

    [Fact]
    public void CalculateWAC_ZeroAddedQty_ShouldReturnCurrentAvgCost()
    {
        var result = _sut.CalculateWAC(50, 20m, 0, 0m);

        result.Should().Be(20m);
    }

    [Fact]
    public void CalculateWAC_BothZero_ShouldReturnZero()
    {
        var result = _sut.CalculateWAC(0, 0m, 0, 0m);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateWAC_NegativeResultingStock_ShouldReturnZero()
    {
        // currentStock + addedQty <= 0
        var result = _sut.CalculateWAC(5, 10m, -10, 0m);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateWAC_LargeValues_ShouldNotOverflow()
    {
        var result = _sut.CalculateWAC(1_000_000, 100m, 500_000, 120m);

        // (1M * 100 + 500K * 120) / 1.5M = (100M + 60M) / 1.5M = ~106.67
        result.Should().BeApproximately(106.67m, 0.01m);
    }

    // ── ValidateStockSufficiency Edge Cases ──

    [Fact]
    public void ValidateStockSufficiency_ExactStock_ShouldNotThrow()
    {
        var product = FakeData.CreateProduct(stock: 10);

        var act = () => _sut.ValidateStockSufficiency(product, 10);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateStockSufficiency_ZeroRequired_ShouldNotThrow()
    {
        var product = FakeData.CreateProduct(stock: 0);

        var act = () => _sut.ValidateStockSufficiency(product, 0);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateStockSufficiency_InsufficientStock_ShouldThrowWithDetails()
    {
        var product = FakeData.CreateProduct(sku: "INSUF-EDGE", stock: 3);

        var act = () => _sut.ValidateStockSufficiency(product, 10);

        act.Should().Throw<InsufficientStockException>()
            .Where(ex => ex.SKU == "INSUF-EDGE"
                      && ex.AvailableStock == 3
                      && ex.RequestedQuantity == 10);
    }

    // ── SelectLotsForConsumption Edge Cases ──

    [Fact]
    public void SelectLotsForConsumption_EmptyList_ShouldReturnEmpty()
    {
        var result = _sut.SelectLotsForConsumption(Enumerable.Empty<InventoryLot>(), 100);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SelectLotsForConsumption_ZeroRequired_ShouldReturnEmpty()
    {
        var productId = Guid.NewGuid();
        var lots = new List<InventoryLot>
        {
            FakeData.CreateLot(productId, remainingQty: 50)
        };

        var result = _sut.SelectLotsForConsumption(lots, 0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SelectLotsForConsumption_ShouldSelectLotsInFEFOOrder()
    {
        var productId = Guid.NewGuid();
        var lot1 = FakeData.CreateLot(productId, remainingQty: 30,
            expiryDate: DateTime.UtcNow.AddMonths(3));
        var lot2 = FakeData.CreateLot(productId, remainingQty: 50,
            expiryDate: DateTime.UtcNow.AddMonths(1)); // expires sooner

        var result = _sut.SelectLotsForConsumption(new[] { lot1, lot2 }, 25);

        // Should select lot2 first (earlier expiry), and lot2 alone covers 25
        result.Should().HaveCount(1);
        result[0].Should().Be(lot2);
    }

    [Fact]
    public void SelectLotsForConsumption_ShouldSkipClosedLots()
    {
        var productId = Guid.NewGuid();
        var closedLot = FakeData.CreateLot(productId, remainingQty: 100);
        closedLot.Status = LotStatus.Closed;

        var openLot = FakeData.CreateLot(productId, remainingQty: 50);

        var result = _sut.SelectLotsForConsumption(new[] { closedLot, openLot }, 30);

        result.Should().HaveCount(1);
        result[0].Should().Be(openLot);
    }

    [Fact]
    public void SelectLotsForConsumption_ShouldSkipZeroRemainingLots()
    {
        var productId = Guid.NewGuid();
        var emptyLot = FakeData.CreateLot(productId, remainingQty: 0);
        var fullLot = FakeData.CreateLot(productId, remainingQty: 100);

        var result = _sut.SelectLotsForConsumption(new[] { emptyLot, fullLot }, 50);

        result.Should().HaveCount(1);
        result[0].Should().Be(fullLot);
    }

    // ── CalculateInventoryValue Edge Cases ──

    [Fact]
    public void CalculateInventoryValue_EmptyProducts_ShouldReturnZero()
    {
        var result = _sut.CalculateInventoryValue(Enumerable.Empty<Product>());

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateInventoryValue_ZeroStock_ShouldReturnZero()
    {
        var products = new[]
        {
            FakeData.CreateProduct(stock: 0, purchasePrice: 100m)
        };

        var result = _sut.CalculateInventoryValue(products);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateInventoryValue_MultipleProducts_ShouldSumCorrectly()
    {
        var products = new[]
        {
            FakeData.CreateProduct(stock: 10, purchasePrice: 50m),
            FakeData.CreateProduct(stock: 5, purchasePrice: 200m)
        };

        var result = _sut.CalculateInventoryValue(products);

        // 10*50 + 5*200 = 500 + 1000 = 1500
        result.Should().Be(1500m);
    }
}
