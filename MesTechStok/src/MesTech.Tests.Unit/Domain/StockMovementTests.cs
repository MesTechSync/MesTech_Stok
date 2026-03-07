using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class StockMovementTests
{
    // ── Computed Properties ──

    [Fact]
    public void IsPositiveMovement_WhenQuantityPositive_ShouldReturnTrue()
    {
        var movement = new StockMovement { Quantity = 50 };

        movement.IsPositiveMovement.Should().BeTrue();
        movement.IsNegativeMovement.Should().BeFalse();
    }

    [Fact]
    public void IsPositiveMovement_WhenQuantityNegative_ShouldReturnFalse()
    {
        var movement = new StockMovement { Quantity = -10 };

        movement.IsPositiveMovement.Should().BeFalse();
        movement.IsNegativeMovement.Should().BeTrue();
    }

    [Fact]
    public void IsPositiveMovement_WhenQuantityZero_ShouldReturnFalse()
    {
        var movement = new StockMovement { Quantity = 0 };

        movement.IsPositiveMovement.Should().BeFalse();
        movement.IsNegativeMovement.Should().BeFalse();
    }

    [Fact]
    public void IsNegativeMovement_WhenQuantityNegative_ShouldReturnTrue()
    {
        var movement = new StockMovement { Quantity = -25 };

        movement.IsNegativeMovement.Should().BeTrue();
    }

    // ── SetMovementType ──

    [Fact]
    public void SetMovementType_ShouldConvertEnumToString()
    {
        var movement = new StockMovement();

        movement.SetMovementType(StockMovementType.Purchase);

        movement.MovementType.Should().Be("Purchase");
    }

    // ── ToString ──

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var productId = Guid.NewGuid();
        var movement = new StockMovement
        {
            ProductId = productId,
            Quantity = 100
        };
        movement.SetMovementType(StockMovementType.StockIn);

        var result = movement.ToString();

        result.Should().Be($"StockIn: 100 (Product: {productId})");
    }

    // ── Transfer ──

    [Fact]
    public void Transfer_ShouldHaveBothWarehouseIds()
    {
        var fromWarehouse = Guid.NewGuid();
        var toWarehouse = Guid.NewGuid();
        var movement = new StockMovement
        {
            Quantity = 30,
            FromWarehouseId = fromWarehouse,
            ToWarehouseId = toWarehouse,
            FromLocation = "A-1-3",
            ToLocation = "B-2-1"
        };
        movement.SetMovementType(StockMovementType.Transfer);

        movement.FromWarehouseId.Should().Be(fromWarehouse);
        movement.ToWarehouseId.Should().Be(toWarehouse);
        movement.MovementType.Should().Be("Transfer");
    }

    // ── Scanned Movement ──

    [Fact]
    public void ScannedMovement_ShouldSetBarcodeAndFlag()
    {
        var movement = new StockMovement
        {
            Quantity = 1,
            ScannedBarcode = "8680000000123",
            IsScannedMovement = true
        };
        movement.SetMovementType(StockMovementType.BarcodeSale);

        movement.ScannedBarcode.Should().Be("8680000000123");
        movement.IsScannedMovement.Should().BeTrue();
        movement.MovementType.Should().Be("BarcodeSale");
    }

    // ── Reversal ──

    [Fact]
    public void Reversal_ShouldLinkToOriginalMovement()
    {
        var originalId = Guid.NewGuid();
        var reversal = new StockMovement
        {
            Quantity = 10,
            IsReversed = true,
            ReversalMovementId = originalId
        };

        reversal.IsReversed.Should().BeTrue();
        reversal.ReversalMovementId.Should().Be(originalId);
    }

    // ── Approval ──

    [Fact]
    public void Approval_ShouldSetApproverAndDate()
    {
        var now = DateTime.UtcNow;
        var movement = new StockMovement
        {
            Quantity = 500,
            IsApproved = true,
            ApprovedBy = "admin@mestech.com",
            ApprovedDate = now
        };

        movement.IsApproved.Should().BeTrue();
        movement.ApprovedBy.Should().Be("admin@mestech.com");
        movement.ApprovedDate.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    // ── Negative Stock Control (User-requested: documents domain behavior) ──

    [Fact]
    public void NegativeStockControl_WhenWithdrawingMoreThanAvailable_StockGoesNegative()
    {
        // ARRANGE: Product with 10 units in stock
        var product = FakeData.CreateProduct(sku: "NEG-001", stock: 10);
        product.Stock.Should().Be(10);

        // ACT: Withdraw 25 units (more than available)
        // Product.AdjustStock does Stock += quantity with no guard clause.
        // Passing -25 means stock becomes 10 + (-25) = -15.
        product.AdjustStock(-25, StockMovementType.Sale, "Test excess withdrawal");

        // ASSERT: Stock goes negative — this is the current domain behavior.
        // No exception is thrown. This documents that the domain ALLOWS negative stock.
        // If a business rule change requires preventing negative stock,
        // this test must be updated to expect an exception or validation error.
        product.Stock.Should().Be(-15);
        product.IsOutOfStock().Should().BeTrue();

        // Domain event should still be raised even for negative stock
        product.DomainEvents.Should().HaveCount(1);
        var @event = product.DomainEvents[0] as MesTech.Domain.Events.StockChangedEvent;
        @event.Should().NotBeNull();
        @event!.PreviousQuantity.Should().Be(10);
        @event.NewQuantity.Should().Be(-15);
    }
}
