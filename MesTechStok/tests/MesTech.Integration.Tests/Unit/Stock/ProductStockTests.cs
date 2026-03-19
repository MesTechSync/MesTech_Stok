using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// Product aggregate root stock domain logic unit tests.
/// Covers: AdjustStock, IsLowStock, IsOutOfStock, IsOverStock, NeedsReorder,
///         TotalValue, ProfitMargin, domain events (StockChanged, LowStockDetected).
/// InventoryLot: Consume, IsExpired.
/// </summary>
public class ProductStockTests
{
    private static Product CreateProduct(int stock = 100, int minimumStock = 10, int maximumStock = 1000,
        int reorderLevel = 10, decimal purchasePrice = 50m, decimal salePrice = 100m)
    {
        return new Product
        {
            TenantId = Guid.NewGuid(),
            Name = "Test Product",
            SKU = "TST-001",
            Stock = stock,
            MinimumStock = minimumStock,
            MaximumStock = maximumStock,
            ReorderLevel = reorderLevel,
            PurchasePrice = purchasePrice,
            SalePrice = salePrice,
            CategoryId = Guid.NewGuid()
        };
    }

    // ── 1. AdjustStock increases quantity ──

    [Fact]
    public void Product_AdjustStock_IncreasesQuantity()
    {
        // Arrange
        var product = CreateProduct(stock: 100);

        // Act
        product.AdjustStock(50, StockMovementType.StockIn, "Restock");

        // Assert
        product.Stock.Should().Be(150);
        product.LastStockUpdate.Should().NotBeNull();
    }

    // ── 2. AdjustStock decreases quantity ──

    [Fact]
    public void Product_AdjustStock_DecreasesQuantity()
    {
        // Arrange
        var product = CreateProduct(stock: 100);

        // Act
        product.AdjustStock(-30, StockMovementType.Sale, "Customer order");

        // Assert
        product.Stock.Should().Be(70);
    }

    // ── 3. IsLowStock returns true when stock <= minimum ──

    [Fact]
    public void Product_IsLowStock_True_WhenBelowMinimum()
    {
        // Arrange
        var product = CreateProduct(stock: 5, minimumStock: 10);

        // Act & Assert
        product.IsLowStock().Should().BeTrue();
    }

    // ── 4. IsLowStock returns false when stock > minimum ──

    [Fact]
    public void Product_IsLowStock_False_WhenAboveMinimum()
    {
        // Arrange
        var product = CreateProduct(stock: 50, minimumStock: 10);

        // Act & Assert
        product.IsLowStock().Should().BeFalse();
    }

    // ── 5. IsOutOfStock returns true when stock = 0 ──

    [Fact]
    public void Product_IsOutOfStock_True_WhenZero()
    {
        // Arrange
        var product = CreateProduct(stock: 0);

        // Act & Assert
        product.IsOutOfStock().Should().BeTrue();
    }

    // ── 6. IsOutOfStock returns false when stock > 0 ──

    [Fact]
    public void Product_IsOutOfStock_False_WhenPositive()
    {
        // Arrange
        var product = CreateProduct(stock: 1);

        // Act & Assert
        product.IsOutOfStock().Should().BeFalse();
    }

    // ── 7. TotalValue calculated correctly ──

    [Fact]
    public void Product_TotalValue_CalculatedCorrectly()
    {
        // Arrange
        var product = CreateProduct(stock: 10, purchasePrice: 50m);

        // Act & Assert
        product.TotalValue.Should().Be(500m);
    }

    // ── 8. AdjustStock raises StockChangedEvent ──

    [Fact]
    public void Product_AdjustStock_RaisesStockChangedEvent()
    {
        // Arrange
        var product = CreateProduct(stock: 100);

        // Act
        product.AdjustStock(10, StockMovementType.Purchase);

        // Assert
        product.DomainEvents.Should().ContainSingle(e => e is StockChangedEvent);
        var evt = product.DomainEvents.OfType<StockChangedEvent>().First();
        evt.PreviousQuantity.Should().Be(100);
        evt.NewQuantity.Should().Be(110);
        evt.MovementType.Should().Be(StockMovementType.Purchase);
        evt.SKU.Should().Be("TST-001");
    }

    // ── 9. AdjustStock crossing low threshold raises LowStockDetectedEvent ──

    [Fact]
    public void Product_AdjustStock_CrossingLowThreshold_RaisesLowStockDetectedEvent()
    {
        // Arrange — stock=11, minimumStock=10, so previousStock > MinimumStock
        var product = CreateProduct(stock: 11, minimumStock: 10);

        // Act — decrease by 2 → stock=9, now IsLowStock()=true AND previousStock(11) > MinimumStock(10)
        product.AdjustStock(-2, StockMovementType.Sale);

        // Assert
        product.Stock.Should().Be(9);
        product.DomainEvents.Should().Contain(e => e is LowStockDetectedEvent);
        var evt = product.DomainEvents.OfType<LowStockDetectedEvent>().First();
        evt.CurrentStock.Should().Be(9);
        evt.MinimumStock.Should().Be(10);
    }

    // ── 10. AdjustStock already below minimum does NOT raise LowStockDetectedEvent ──

    [Fact]
    public void Product_AdjustStock_AlreadyBelowMinimum_DoesNotRaiseLowStockEvent()
    {
        // Arrange — already below minimum
        var product = CreateProduct(stock: 5, minimumStock: 10);

        // Act
        product.AdjustStock(-1, StockMovementType.Sale);

        // Assert — StockChangedEvent should exist, but NOT LowStockDetectedEvent
        product.DomainEvents.Should().ContainSingle(e => e is StockChangedEvent);
        product.DomainEvents.Should().NotContain(e => e is LowStockDetectedEvent);
    }

    // ── 11. InventoryLot Consume reduces remaining quantity ──

    [Fact]
    public void InventoryLot_Consume_ReducesRemainingQty()
    {
        // Arrange
        var lot = new InventoryLot
        {
            TenantId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            LotNumber = "LOT-001",
            ReceivedQty = 100,
            RemainingQty = 100,
            Status = LotStatus.Open
        };

        // Act
        lot.Consume(30);

        // Assert
        lot.RemainingQty.Should().Be(70);
        lot.Status.Should().Be(LotStatus.Open);
    }

    // ── 12. InventoryLot Consume exceeds remaining throws ──

    [Fact]
    public void InventoryLot_Consume_ExceedsRemaining_ThrowsInvalidOperation()
    {
        // Arrange
        var lot = new InventoryLot
        {
            TenantId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            LotNumber = "LOT-002",
            ReceivedQty = 100,
            RemainingQty = 10
        };

        // Act & Assert
        var act = () => lot.Consume(20);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot consume*only*remaining*");
    }

    // ── 13. InventoryLot IsExpired true when past expiry ──

    [Fact]
    public void InventoryLot_IsExpired_True_WhenPastExpiry()
    {
        // Arrange
        var lot = new InventoryLot
        {
            TenantId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            LotNumber = "LOT-003",
            ExpiryDate = DateTime.UtcNow.AddDays(-1),
            ReceivedQty = 50,
            RemainingQty = 50
        };

        // Act & Assert
        lot.IsExpired.Should().BeTrue();
    }

    // ── 14. InventoryLot IsExpired false when future expiry ──

    [Fact]
    public void InventoryLot_IsExpired_False_WhenFutureExpiry()
    {
        // Arrange
        var lot = new InventoryLot
        {
            TenantId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            LotNumber = "LOT-004",
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            ReceivedQty = 50,
            RemainingQty = 50
        };

        // Act & Assert
        lot.IsExpired.Should().BeFalse();
    }

    // ── 15. InventoryLot Consume all closes the lot ──

    [Fact]
    public void InventoryLot_ConsumeAll_ClosesLot()
    {
        // Arrange
        var lot = new InventoryLot
        {
            TenantId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            LotNumber = "LOT-005",
            ReceivedQty = 50,
            RemainingQty = 50,
            Status = LotStatus.Open
        };

        // Act
        lot.Consume(50);

        // Assert
        lot.RemainingQty.Should().Be(0);
        lot.Status.Should().Be(LotStatus.Closed);
        lot.ClosedDate.Should().NotBeNull();
    }

    // ── 16. Product IsOverStock when above maximum ──

    [Fact]
    public void Product_IsOverStock_True_WhenAboveMaximum()
    {
        // Arrange
        var product = CreateProduct(stock: 1500, maximumStock: 1000);

        // Act & Assert
        product.IsOverStock().Should().BeTrue();
    }

    // ── 17. Product NeedsReorder when at reorder level ──

    [Fact]
    public void Product_NeedsReorder_True_WhenAtReorderLevel()
    {
        // Arrange
        var product = CreateProduct(stock: 10, reorderLevel: 10);

        // Act & Assert
        product.NeedsReorder().Should().BeTrue();
    }

    // ── 18. Product ProfitMargin calculated correctly ──

    [Fact]
    public void Product_ProfitMargin_CalculatedCorrectly()
    {
        // Arrange — purchase=50, sale=100 → margin = ((100-50)/100)*100 = 50%
        var product = CreateProduct(purchasePrice: 50m, salePrice: 100m);

        // Act & Assert
        product.ProfitMargin.Should().Be(50m);
    }
}
