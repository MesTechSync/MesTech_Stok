using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Exceptions;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// Product entity domain behavior tests.
/// AdjustStock, UpdatePrice, AddStock, RemoveStock, state flags, domain events.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ProductEntity")]
[Trait("Phase", "Dalga15")]
public class ProductEntityTests
{
    private static Product CreateProduct(int stock = 100, decimal salePrice = 50m, decimal purchasePrice = 30m)
    {
        var product = new Product
        {
            TenantId = Guid.NewGuid(),
            Name = "Test Product",
            SKU = "TST-001",
            SalePrice = salePrice,
            PurchasePrice = purchasePrice,
            MinimumStock = 5,
            MaximumStock = 1000,
            ReorderLevel = 10,
            ReorderQuantity = 50,
            TaxRate = 0.18m
        };
        product.SyncStock(stock, "test-seed");
        return product;
    }

    // ═══════════════════════════════════════════
    // AdjustStock Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void AdjustStock_PositiveQuantity_IncreasesStock()
    {
        var product = CreateProduct(stock: 50);

        product.AdjustStock(20, StockMovementType.StockIn, "test");

        product.Stock.Should().Be(70);
    }

    [Fact]
    public void AdjustStock_NegativeQuantity_DecreasesStock()
    {
        var product = CreateProduct(stock: 50);

        product.AdjustStock(-10, StockMovementType.StockOut, "test");

        product.Stock.Should().Be(40);
    }

    [Fact]
    public void AdjustStock_ZeroQuantity_StockUnchanged()
    {
        var product = CreateProduct(stock: 50);

        product.AdjustStock(0, StockMovementType.Adjustment, "zero adjust");

        product.Stock.Should().Be(50);
    }

    [Fact]
    public void AdjustStock_NegativeWouldGoBelow_ThrowsInsufficientStockException()
    {
        var product = CreateProduct(stock: 5);

        var act = () => product.AdjustStock(-10, StockMovementType.StockOut);

        act.Should().Throw<InsufficientStockException>()
            .Which.SKU.Should().Be("TST-001");
    }

    [Fact]
    public void AdjustStock_ExactStockRemoval_SetsStockToZero()
    {
        var product = CreateProduct(stock: 10);

        product.AdjustStock(-10, StockMovementType.StockOut, "full removal");

        product.Stock.Should().Be(0);
    }

    [Fact]
    public void AdjustStock_SetsLastStockUpdate()
    {
        var product = CreateProduct(stock: 50);
        var before = DateTime.UtcNow;

        product.AdjustStock(5, StockMovementType.StockIn);

        product.LastStockUpdate.Should().NotBeNull();
        product.LastStockUpdate!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void AdjustStock_RaisesStockChangedEvent()
    {
        var product = CreateProduct(stock: 50);
        product.ClearDomainEvents();

        product.AdjustStock(10, StockMovementType.StockIn);

        product.DomainEvents.Should().ContainSingle(e => e is StockChangedEvent);
        var evt = product.DomainEvents.OfType<StockChangedEvent>().First();
        evt.PreviousQuantity.Should().Be(50);
        evt.NewQuantity.Should().Be(60);
    }

    [Fact]
    public void AdjustStock_DropsToZero_RaisesZeroStockDetectedEvent()
    {
        var product = CreateProduct(stock: 5);
        product.ClearDomainEvents();

        product.AdjustStock(-5, StockMovementType.StockOut);

        product.DomainEvents.Should().Contain(e => e is ZeroStockDetectedEvent);
    }

    [Fact]
    public void AdjustStock_DropsBelowMinimum_RaisesLowStockDetectedEvent()
    {
        // Stock=20, MinimumStock=5. Go from 20 to 4 (below minimum, was above)
        var product = CreateProduct(stock: 20);
        product.ClearDomainEvents();

        product.AdjustStock(-16, StockMovementType.StockOut);

        product.DomainEvents.Should().Contain(e => e is LowStockDetectedEvent);
    }

    [Fact]
    public void AdjustStock_StockAlreadyLow_DoesNotRaiseLowStockAgain()
    {
        // Stock=3 (already below minimum=5)
        var product = CreateProduct(stock: 3);
        product.ClearDomainEvents();

        product.AdjustStock(-1, StockMovementType.StockOut);

        // Should NOT raise LowStockDetectedEvent because previousStock was already <= MinimumStock
        product.DomainEvents.Should().NotContain(e => e is LowStockDetectedEvent);
    }

    // ═══════════════════════════════════════════
    // UpdatePrice Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void UpdatePrice_ValidPrice_UpdatesSalePrice()
    {
        var product = CreateProduct(salePrice: 50m);

        product.UpdatePrice(75m);

        product.SalePrice.Should().Be(75m);
    }

    [Fact]
    public void UpdatePrice_NegativePrice_ThrowsArgumentException()
    {
        var product = CreateProduct();

        var act = () => product.UpdatePrice(-10m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdatePrice_SamePrice_NoEventRaised()
    {
        var product = CreateProduct(salePrice: 50m);
        product.ClearDomainEvents();

        product.UpdatePrice(50m);

        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdatePrice_RaisesPriceChangedEvent()
    {
        var product = CreateProduct(salePrice: 50m);
        product.ClearDomainEvents();

        product.UpdatePrice(60m);

        product.DomainEvents.Should().ContainSingle(e => e is PriceChangedEvent);
        var evt = product.DomainEvents.OfType<PriceChangedEvent>().First();
        evt.OldPrice.Should().Be(50m);
        evt.NewPrice.Should().Be(60m);
    }

    [Fact]
    public void UpdatePrice_BelowPurchasePrice_RaisesPriceLossDetectedEvent()
    {
        var product = CreateProduct(salePrice: 50m, purchasePrice: 30m);
        product.ClearDomainEvents();

        product.UpdatePrice(25m);

        product.DomainEvents.Should().Contain(e => e is PriceLossDetectedEvent);
        var evt = product.DomainEvents.OfType<PriceLossDetectedEvent>().First();
        evt.LossPerUnit.Should().Be(5m); // 30 - 25
    }

    [Fact]
    public void UpdatePrice_ZeroPrice_Accepted()
    {
        var product = CreateProduct(salePrice: 50m);

        product.UpdatePrice(0m);

        product.SalePrice.Should().Be(0m);
    }

    // ═══════════════════════════════════════════
    // AddStock / RemoveStock Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void AddStock_PositiveQuantity_IncreasesStock()
    {
        var product = CreateProduct(stock: 10);

        product.AddStock(5, "purchase");

        product.Stock.Should().Be(15);
    }

    [Fact]
    public void AddStock_ZeroQuantity_ThrowsArgumentException()
    {
        var product = CreateProduct();

        var act = () => product.AddStock(0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddStock_NegativeQuantity_ThrowsArgumentException()
    {
        var product = CreateProduct();

        var act = () => product.AddStock(-5);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveStock_ValidQuantity_DecreasesStock()
    {
        var product = CreateProduct(stock: 20);

        product.RemoveStock(5, "sale");

        product.Stock.Should().Be(15);
    }

    [Fact]
    public void RemoveStock_MoreThanAvailable_ThrowsInsufficientStockException()
    {
        var product = CreateProduct(stock: 3);

        var act = () => product.RemoveStock(10);

        act.Should().Throw<InsufficientStockException>();
    }

    [Fact]
    public void RemoveStock_ZeroQuantity_ThrowsArgumentException()
    {
        var product = CreateProduct();

        var act = () => product.RemoveStock(0);

        act.Should().Throw<ArgumentException>();
    }

    // ═══════════════════════════════════════════
    // State Flags & Computed Properties Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void IsLowStock_WhenStockBelowMinimum_ReturnsTrue()
    {
        var product = CreateProduct(stock: 3);
        product.IsLowStock().Should().BeTrue();
    }

    [Fact]
    public void IsLowStock_WhenStockAboveMinimum_ReturnsFalse()
    {
        var product = CreateProduct(stock: 50);
        product.IsLowStock().Should().BeFalse();
    }

    [Fact]
    public void IsOutOfStock_WhenStockIsZero_ReturnsTrue()
    {
        var product = CreateProduct(stock: 0);
        product.IsOutOfStock().Should().BeTrue();
    }

    [Fact]
    public void IsOverStock_WhenAboveMax_ReturnsTrue()
    {
        var product = CreateProduct(stock: 1500);
        product.IsOverStock().Should().BeTrue();
    }

    [Fact]
    public void IsCriticalStock_WhenBelowMinButAboveZero_ReturnsTrue()
    {
        var product = CreateProduct(stock: 3);
        product.IsCriticalStock.Should().BeTrue();
    }

    [Fact]
    public void IsCriticalStock_WhenZero_ReturnsFalse()
    {
        var product = CreateProduct(stock: 0);
        product.IsCriticalStock.Should().BeFalse();
    }

    [Fact]
    public void NeedsReorder_WhenBelowReorderLevel_ReturnsTrue()
    {
        var product = CreateProduct(stock: 8);
        product.NeedsReorder().Should().BeTrue();
    }

    [Fact]
    public void ProfitMargin_CalculatesCorrectly()
    {
        var product = CreateProduct(salePrice: 100m, purchasePrice: 60m);
        product.ProfitMargin.Should().Be(40m);
    }

    [Fact]
    public void TotalValue_CalculatesStockTimesPrice()
    {
        var product = CreateProduct(stock: 10, purchasePrice: 25m);
        product.TotalValue.Should().Be(250m);
    }

    // ═══════════════════════════════════════════
    // Activate / Deactivate Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var product = CreateProduct();
        product.Deactivate();
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var product = CreateProduct();
        product.Deactivate();
        product.Activate();
        product.IsActive.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // AI Snapshot Tests
    // ═══════════════════════════════════════════

    [Fact]
    public void UpdateAiPriceSnapshot_SetsRecommendedPrice()
    {
        var product = CreateProduct();

        product.UpdateAiPriceSnapshot(99.99m);

        product.RecommendedPrice.Should().Be(99.99m);
        product.LastAiPriceAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateAiStockSnapshot_SetsPredictedDemand()
    {
        var product = CreateProduct();

        product.UpdateAiStockSnapshot(42, 7);

        product.PredictedDemand7d.Should().Be(42);
        product.DaysUntilStockout.Should().Be(7);
        product.LastAiStockAt.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════
    // MarkAsCreated / MarkAsUpdated / ToString
    // ═══════════════════════════════════════════

    [Fact]
    public void MarkAsCreated_RaisesProductCreatedEvent()
    {
        var product = CreateProduct();
        product.ClearDomainEvents();

        product.MarkAsCreated();

        product.DomainEvents.Should().ContainSingle(e => e is ProductCreatedEvent);
    }

    [Fact]
    public void MarkAsUpdated_RaisesProductUpdatedEvent()
    {
        var product = CreateProduct();
        product.ClearDomainEvents();

        product.MarkAsUpdated();

        product.DomainEvents.Should().ContainSingle(e => e is ProductUpdatedEvent);
    }

    [Fact]
    public void ToString_ContainsSKUAndName()
    {
        var product = CreateProduct();

        product.ToString().Should().Contain("TST-001");
        product.ToString().Should().Contain("Test Product");
    }
}
