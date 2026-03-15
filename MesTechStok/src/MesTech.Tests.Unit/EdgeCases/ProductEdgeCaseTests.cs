using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// Product entity edge case tests — boundary values, overstock, price changes, volume calc.
/// Dalga 14 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class ProductEdgeCaseTests
{
    [Fact]
    public void AdjustStock_ToNegativeStock_ShouldAllowNegativeValue()
    {
        // Domain allows negative stock (oversold scenario) — caller decides policy
        var product = FakeData.CreateProduct(stock: 5);

        product.AdjustStock(-10, StockMovementType.StockOut);

        product.Stock.Should().Be(-5);
        product.IsOutOfStock().Should().BeTrue();
    }

    [Fact]
    public void AdjustStock_IntMaxValue_ShouldNotOverflow()
    {
        var product = FakeData.CreateProduct(stock: 0);

        product.AdjustStock(int.MaxValue, StockMovementType.StockIn);

        product.Stock.Should().Be(int.MaxValue);
    }

    [Fact]
    public void AdjustStock_ShouldRaiseLowStockEvent_WhenCrossesThreshold()
    {
        var product = FakeData.CreateProduct(stock: 10, minimumStock: 5);

        product.AdjustStock(-6, StockMovementType.StockOut);

        // Should have StockChanged + LowStockDetected events
        product.DomainEvents.Should().HaveCount(2);
        product.DomainEvents[1].Should().BeOfType<LowStockDetectedEvent>();
    }

    [Fact]
    public void AdjustStock_ShouldNotRaiseLowStockEvent_WhenAlreadyBelowMinimum()
    {
        var product = FakeData.CreateProduct(stock: 3, minimumStock: 5);

        product.AdjustStock(-1, StockMovementType.StockOut);

        // Only StockChanged, no LowStockDetected (already below)
        product.DomainEvents.Should().HaveCount(1);
        product.DomainEvents[0].Should().BeOfType<StockChangedEvent>();
    }

    [Fact]
    public void UpdatePrice_SamePrice_ShouldNotRaiseEvent()
    {
        var product = FakeData.CreateProduct(salePrice: 150m);

        product.UpdatePrice(150m);

        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdatePrice_DifferentPrice_ShouldRaisePriceChangedEvent()
    {
        var product = FakeData.CreateProduct(salePrice: 100m);

        product.UpdatePrice(120m);

        product.DomainEvents.Should().ContainSingle();
        var evt = product.DomainEvents[0] as PriceChangedEvent;
        evt.Should().NotBeNull();
        evt!.OldPrice.Should().Be(100m);
        evt.NewPrice.Should().Be(120m);
    }

    [Fact]
    public void UpdatePrice_ToZero_ShouldRaiseEvent()
    {
        var product = FakeData.CreateProduct(salePrice: 50m);

        product.UpdatePrice(0m);

        product.SalePrice.Should().Be(0m);
        product.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void IsOverStock_WhenStockExceedsMaximum_ShouldReturnTrue()
    {
        var product = FakeData.CreateProduct(stock: 1500);
        product.MaximumStock = 1000;

        product.IsOverStock().Should().BeTrue();
    }

    [Fact]
    public void IsOverStock_WhenMaximumStockIsZero_ShouldReturnFalse()
    {
        var product = FakeData.CreateProduct(stock: 100);
        product.MaximumStock = 0;

        product.IsOverStock().Should().BeFalse();
    }

    [Fact]
    public void Volume_WithAllDimensions_ShouldCalculateCorrectly()
    {
        var product = FakeData.CreateProduct();
        product.Length = 10m;
        product.Width = 5m;
        product.Height = 2m;

        product.Volume.Should().Be(100m);
    }

    [Fact]
    public void Volume_WithMissingDimension_ShouldReturnNull()
    {
        var product = FakeData.CreateProduct();
        product.Length = 10m;
        product.Width = 5m;
        product.Height = null;

        product.Volume.Should().BeNull();
    }

    [Fact]
    public void ProfitMargin_WhenBothZero_ShouldReturnZero()
    {
        var product = FakeData.CreateProduct(purchasePrice: 0, salePrice: 0);

        product.ProfitMargin.Should().Be(0);
    }

    [Fact]
    public void ProfitMargin_WhenPurchasePriceExceedsSalePrice_ShouldBeNegative()
    {
        var product = FakeData.CreateProduct(purchasePrice: 150m, salePrice: 100m);

        // (100-150)/100 * 100 = -50
        product.ProfitMargin.Should().Be(-50m);
    }

    [Fact]
    public void ToString_ShouldContainSKUAndName()
    {
        var product = FakeData.CreateProduct(sku: "TST-001");
        product.Name = "Test Product";

        var str = product.ToString();

        str.Should().Contain("TST-001");
        str.Should().Contain("Test Product");
    }

    [Fact]
    public void NeedsReorder_AtExactReorderLevel_ShouldReturnTrue()
    {
        var product = FakeData.CreateProduct(stock: 10);
        product.ReorderLevel = 10;

        product.NeedsReorder().Should().BeTrue();
    }

    [Fact]
    public void IsLowStock_AtExactMinimumStock_ShouldReturnTrue()
    {
        var product = FakeData.CreateProduct(stock: 5, minimumStock: 5);

        product.IsLowStock().Should().BeTrue();
    }

    [Fact]
    public void AdjustStock_ShouldUpdateLastStockUpdateTimestamp()
    {
        var product = FakeData.CreateProduct(stock: 50);
        product.LastStockUpdate = null;

        product.AdjustStock(10, StockMovementType.StockIn);

        product.LastStockUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
