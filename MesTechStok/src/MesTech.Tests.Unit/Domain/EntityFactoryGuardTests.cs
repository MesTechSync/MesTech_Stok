using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Dropshipping.Enums;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

// ════════════════════════════════════════════════════════
// DEV5 TUR 12: G467 — Factory validation edge-case tests
// BankTransaction, DropshipOrder, StockPlacement
// DEV1 guard ekledi → DEV5 edge-case test yazıyor
// ════════════════════════════════════════════════════════

#region BankTransaction

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class BankTransactionDomainTests
{
    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var tx = BankTransaction.Create(Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow, 150.50m, "Kira ödemesi");
        tx.Amount.Should().Be(150.50m);
        tx.IsReconciled.Should().BeFalse();
        tx.IdempotencyKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_EmptyTenantId_Throws()
    {
        var act = () => BankTransaction.Create(Guid.Empty, Guid.NewGuid(), DateTime.UtcNow, 100m, "Test");
        act.Should().Throw<ArgumentException>().WithParameterName("tenantId");
    }

    [Fact]
    public void Create_EmptyBankAccountId_Throws()
    {
        var act = () => BankTransaction.Create(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, 100m, "Test");
        act.Should().Throw<ArgumentException>().WithParameterName("bankAccountId");
    }

    [Fact]
    public void Create_ZeroAmount_Throws()
    {
        var act = () => BankTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 0m, "Test");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NegativeAmount_Succeeds()
    {
        // Negative = withdrawal, should be valid
        var tx = BankTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, -500m, "Çekim");
        tx.Amount.Should().Be(-500m);
    }

    [Fact]
    public void Create_EmptyDescription_Throws()
    {
        var act = () => BankTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 100m, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkReconciled_SetsFlag()
    {
        var tx = BankTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 100m, "Test");
        tx.MarkReconciled();
        tx.IsReconciled.Should().BeTrue();
    }
}

#endregion

#region DropshipOrder — Full State Machine

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class DropshipOrderDomainTests
{
    private static DropshipOrder CreateOrder() =>
        DropshipOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    // ── Factory guards ──

    [Fact]
    public void Create_ValidParams_StartsPending()
    {
        var order = CreateOrder();
        order.Status.Should().Be(DropshipOrderStatus.Pending);
    }

    [Fact]
    public void Create_EmptyTenantId_Throws()
    {
        var act = () => DropshipOrder.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*TenantId*");
    }

    [Fact]
    public void Create_EmptyOrderId_Throws()
    {
        var act = () => DropshipOrder.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*OrderId*");
    }

    [Fact]
    public void Create_EmptySupplierId_Throws()
    {
        var act = () => DropshipOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*SupplierId*");
    }

    [Fact]
    public void Create_EmptyProductId_Throws()
    {
        var act = () => DropshipOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("*ProductId*");
    }

    // ── State transitions ──

    [Fact]
    public void PlaceWithSupplier_FromPending_Succeeds()
    {
        var order = CreateOrder();
        order.PlaceWithSupplier("SUP-REF-001");
        order.Status.Should().Be(DropshipOrderStatus.OrderedFromSupplier);
        order.SupplierOrderRef.Should().Be("SUP-REF-001");
        order.OrderedAt.Should().NotBeNull();
    }

    [Fact]
    public void PlaceWithSupplier_FromShipped_Throws()
    {
        var order = CreateOrder();
        order.PlaceWithSupplier("REF-1");
        order.MarkShipped("TRK-1");
        var act = () => order.PlaceWithSupplier("REF-2");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkShipped_FromOrdered_Succeeds()
    {
        var order = CreateOrder();
        order.PlaceWithSupplier("REF-1");
        order.MarkShipped("TRK-12345");
        order.Status.Should().Be(DropshipOrderStatus.Shipped);
        order.SupplierTrackingNumber.Should().Be("TRK-12345");
    }

    [Fact]
    public void MarkShipped_FromPending_Throws()
    {
        var order = CreateOrder();
        var act = () => order.MarkShipped("TRK-1");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkDelivered_FromShipped_Succeeds()
    {
        var order = CreateOrder();
        order.PlaceWithSupplier("REF");
        order.MarkShipped("TRK");
        order.MarkDelivered();
        order.Status.Should().Be(DropshipOrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_FromPending_Succeeds()
    {
        var order = CreateOrder();
        order.MarkFailed("Stok yok");
        order.Status.Should().Be(DropshipOrderStatus.Failed);
        order.FailureReason.Should().Be("Stok yok");
    }

    [Fact]
    public void MarkFailed_FromDelivered_Throws()
    {
        var order = CreateOrder();
        order.PlaceWithSupplier("R");
        order.MarkShipped("T");
        order.MarkDelivered();
        var act = () => order.MarkFailed("reason");
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Full lifecycle ──

    [Fact]
    public void FullLifecycle_Pending_Ordered_Shipped_Delivered()
    {
        var order = CreateOrder();
        order.PlaceWithSupplier("REF-FULL");
        order.MarkShipped("CARGO-123");
        order.MarkDelivered();

        order.Status.Should().Be(DropshipOrderStatus.Delivered);
        order.OrderedAt.Should().NotBeNull();
        order.ShippedAt.Should().NotBeNull();
        order.DeliveredAt.Should().NotBeNull();
    }
}

#endregion

#region StockPlacement

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class StockPlacementDomainTests
{
    private static StockPlacement CreatePlacement(int qty = 50, int min = 10) =>
        StockPlacement.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), qty, min);

    // ── Factory guards ──

    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var sp = CreatePlacement();
        sp.Quantity.Should().Be(50);
        sp.MinimumStock.Should().Be(10);
        sp.StockStatus.Should().Be("YETERLI");
    }

    [Fact]
    public void Create_EmptyTenantId_Throws()
    {
        var act = () => StockPlacement.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 10);
        act.Should().Throw<ArgumentException>().WithMessage("*TenantId*");
    }

    [Fact]
    public void Create_EmptyProductId_Throws()
    {
        var act = () => StockPlacement.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 10);
        act.Should().Throw<ArgumentException>().WithMessage("*ProductId*");
    }

    [Fact]
    public void Create_NegativeQuantity_Throws()
    {
        var act = () => StockPlacement.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── AdjustQuantity ──

    [Fact]
    public void AdjustQuantity_PositiveDelta_Increases()
    {
        var sp = CreatePlacement(50);
        sp.AdjustQuantity(20);
        sp.Quantity.Should().Be(70);
    }

    [Fact]
    public void AdjustQuantity_NegativeDelta_Decreases()
    {
        var sp = CreatePlacement(50);
        sp.AdjustQuantity(-30);
        sp.Quantity.Should().Be(20);
    }

    [Fact]
    public void AdjustQuantity_GoesNegative_Throws()
    {
        var sp = CreatePlacement(10);
        var act = () => sp.AdjustQuantity(-20);
        act.Should().Throw<InvalidOperationException>().WithMessage("*negatif*");
    }

    // ── StockStatus ──

    [Theory]
    [InlineData(0, 10, "TUKENDI")]
    [InlineData(10, 10, "KRITIK")]
    [InlineData(5, 10, "KRITIK")]
    [InlineData(12, 10, "DUSUK")]     // 10 < 12 <= 15 (1.5x)
    [InlineData(50, 10, "YETERLI")]
    public void StockStatus_ReturnsCorrectLabel(int qty, int min, string expected)
    {
        var sp = CreatePlacement(qty, min);
        sp.StockStatus.Should().Be(expected);
    }

    [Fact]
    public void IsCritical_WhenBelowMinimum_ReturnsTrue()
    {
        var sp = CreatePlacement(5, 10);
        sp.IsCritical.Should().BeTrue();
    }

    [Fact]
    public void IsOutOfStock_WhenZero_ReturnsTrue()
    {
        var sp = CreatePlacement(0, 10);
        sp.IsOutOfStock.Should().BeTrue();
    }

    [Fact]
    public void UpdateMinimumStock_SetsValue()
    {
        var sp = CreatePlacement(50, 10);
        sp.UpdateMinimumStock(25);
        sp.MinimumStock.Should().Be(25);
    }

    [Fact]
    public void UpdateMinimumStock_Negative_Throws()
    {
        var sp = CreatePlacement(50, 10);
        var act = () => sp.UpdateMinimumStock(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

#endregion
