using FluentAssertions;
using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using Xunit;

namespace MesTech.Integration.Tests.Domain;

/// <summary>
/// Domain Event record testleri.
/// Her event'in IDomainEvent implement ettigini,
/// immutability'sini ve property degerlerini dogrular.
/// </summary>
public class StockChangedEventTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidData_ShouldSetAllProperties()
    {
        var productId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var evt = new StockChangedEvent(productId, _tenantId, "SKU-001", 10, 5,
            StockMovementType.StockOut, occurredAt);

        evt.ProductId.Should().Be(productId);
        evt.TenantId.Should().Be(_tenantId);
        evt.SKU.Should().Be("SKU-001");
        evt.PreviousQuantity.Should().Be(10);
        evt.NewQuantity.Should().Be(5);
        evt.MovementType.Should().Be(StockMovementType.StockOut);
        evt.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public void ShouldImplement_IDomainEvent()
    {
        var evt = new StockChangedEvent(Guid.NewGuid(), _tenantId, "SKU-X", 0, 1,
            StockMovementType.StockIn, DateTime.UtcNow);

        evt.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var id = Guid.NewGuid();
        var dt = DateTime.UtcNow;

        var a = new StockChangedEvent(id, _tenantId, "SKU-A", 10, 5, StockMovementType.StockOut, dt);
        var b = new StockChangedEvent(id, _tenantId, "SKU-A", 10, 5, StockMovementType.StockOut, dt);

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        var dt = DateTime.UtcNow;

        var a = new StockChangedEvent(Guid.NewGuid(), _tenantId, "SKU-A", 10, 5, StockMovementType.StockOut, dt);
        var b = new StockChangedEvent(Guid.NewGuid(), _tenantId, "SKU-B", 20, 15, StockMovementType.StockIn, dt);

        a.Should().NotBe(b);
    }

    [Theory]
    [InlineData(StockMovementType.StockIn)]
    [InlineData(StockMovementType.StockOut)]
    [InlineData(StockMovementType.Adjustment)]
    [InlineData(StockMovementType.Transfer)]
    public void AllMovementTypes_ShouldBeValid(StockMovementType type)
    {
        var evt = new StockChangedEvent(Guid.NewGuid(), _tenantId, "SKU-MT", 10, 5, type, DateTime.UtcNow);
        evt.MovementType.Should().Be(type);
    }
}

public class OrderPlacedEventTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidData_ShouldSetAllProperties()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var evt = new OrderPlacedEvent(orderId, _tenantId, "ORD-2026-001", customerId, 1250.50m, occurredAt);

        evt.OrderId.Should().Be(orderId);
        evt.TenantId.Should().Be(_tenantId);
        evt.OrderNumber.Should().Be("ORD-2026-001");
        evt.CustomerId.Should().Be(customerId);
        evt.TotalAmount.Should().Be(1250.50m);
        evt.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public void ShouldImplement_IDomainEvent()
    {
        var evt = new OrderPlacedEvent(Guid.NewGuid(), _tenantId, "ORD-X", Guid.NewGuid(), 100m, DateTime.UtcNow);
        evt.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var oid = Guid.NewGuid();
        var cid = Guid.NewGuid();
        var dt = DateTime.UtcNow;

        var a = new OrderPlacedEvent(oid, _tenantId, "ORD-1", cid, 500m, dt);
        var b = new OrderPlacedEvent(oid, _tenantId, "ORD-1", cid, 500m, dt);

        a.Should().Be(b);
    }
}

public class PriceChangedEventTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidData_ShouldSetAllProperties()
    {
        var productId = Guid.NewGuid();
        var dt = DateTime.UtcNow;

        var evt = new PriceChangedEvent(productId, _tenantId, "SKU-PRICE", 99.90m, 79.90m, dt);

        evt.ProductId.Should().Be(productId);
        evt.TenantId.Should().Be(_tenantId);
        evt.SKU.Should().Be("SKU-PRICE");
        evt.OldPrice.Should().Be(99.90m);
        evt.NewPrice.Should().Be(79.90m);
        evt.OccurredAt.Should().Be(dt);
    }

    [Fact]
    public void ShouldImplement_IDomainEvent()
    {
        var evt = new PriceChangedEvent(Guid.NewGuid(), _tenantId, "X", 10m, 20m, DateTime.UtcNow);
        evt.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void PriceIncrease_ShouldBeDetectable()
    {
        var evt = new PriceChangedEvent(Guid.NewGuid(), _tenantId, "SKU-UP", 50m, 75m, DateTime.UtcNow);
        (evt.NewPrice > evt.OldPrice).Should().BeTrue();
    }

    [Fact]
    public void PriceDecrease_ShouldBeDetectable()
    {
        var evt = new PriceChangedEvent(Guid.NewGuid(), _tenantId, "SKU-DOWN", 100m, 80m, DateTime.UtcNow);
        (evt.NewPrice < evt.OldPrice).Should().BeTrue();
    }
}

public class LowStockDetectedEventTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidData_ShouldSetAllProperties()
    {
        var productId = Guid.NewGuid();
        var dt = DateTime.UtcNow;

        var evt = new LowStockDetectedEvent(productId, _tenantId, "SKU-LOW", 3, 10, dt);

        evt.ProductId.Should().Be(productId);
        evt.TenantId.Should().Be(_tenantId);
        evt.SKU.Should().Be("SKU-LOW");
        evt.CurrentStock.Should().Be(3);
        evt.MinimumStock.Should().Be(10);
        evt.OccurredAt.Should().Be(dt);
    }

    [Fact]
    public void ShouldImplement_IDomainEvent()
    {
        var evt = new LowStockDetectedEvent(Guid.NewGuid(), _tenantId, "X", 0, 5, DateTime.UtcNow);
        evt.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void CurrentStock_ShouldBeBelowMinimum()
    {
        var evt = new LowStockDetectedEvent(Guid.NewGuid(), _tenantId, "SKU-LOW", 2, 10, DateTime.UtcNow);
        evt.CurrentStock.Should().BeLessThanOrEqualTo(evt.MinimumStock);
    }
}

public class InvoiceCreatedEventTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidData_ShouldSetAllProperties()
    {
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var dt = DateTime.UtcNow;

        var evt = new InvoiceCreatedEvent(invoiceId, orderId, _tenantId, InvoiceType.EFatura, 1500m, dt);

        evt.InvoiceId.Should().Be(invoiceId);
        evt.OrderId.Should().Be(orderId);
        evt.TenantId.Should().Be(_tenantId);
        evt.Type.Should().Be(InvoiceType.EFatura);
        evt.GrandTotal.Should().Be(1500m);
        evt.OccurredAt.Should().Be(dt);
    }

    [Fact]
    public void ShouldImplement_IDomainEvent()
    {
        var evt = new InvoiceCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), _tenantId,
            InvoiceType.EFatura, 100m, DateTime.UtcNow);
        evt.Should().BeAssignableTo<IDomainEvent>();
    }

    [Theory]
    [InlineData(InvoiceType.EFatura)]
    [InlineData(InvoiceType.EArsiv)]
    [InlineData(InvoiceType.EIrsaliye)]
    public void AllInvoiceTypes_ShouldBeValid(InvoiceType type)
    {
        var evt = new InvoiceCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), _tenantId, type, 100m, DateTime.UtcNow);
        evt.Type.Should().Be(type);
    }
}

public class OrderCancelledEventTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidData_ShouldSetAllProperties()
    {
        var orderId = Guid.NewGuid();
        var dt = DateTime.UtcNow;

        var evt = new OrderCancelledEvent(orderId, _tenantId, "Trendyol", "TY-1001",
            "Musteri vazgecti", dt);

        evt.OrderId.Should().Be(orderId);
        evt.TenantId.Should().Be(_tenantId);
        evt.PlatformCode.Should().Be("Trendyol");
        evt.PlatformOrderId.Should().Be("TY-1001");
        evt.Reason.Should().Be("Musteri vazgecti");
        evt.OccurredAt.Should().Be(dt);
    }

    [Fact]
    public void ShouldImplement_IDomainEvent()
    {
        var evt = new OrderCancelledEvent(Guid.NewGuid(), _tenantId, "N11", "N11-X",
            null, DateTime.UtcNow);
        evt.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void NullReason_ShouldBeAllowed()
    {
        var evt = new OrderCancelledEvent(Guid.NewGuid(), _tenantId, "HB", "HB-123",
            null, DateTime.UtcNow);
        evt.Reason.Should().BeNull();
    }

    [Fact]
    public void WithReason_ShouldStoreReason()
    {
        var evt = new OrderCancelledEvent(Guid.NewGuid(), _tenantId, "CS", "CS-456",
            "Stok tukendi", DateTime.UtcNow);
        evt.Reason.Should().NotBeNull();
        evt.Reason.Should().Contain("Stok");
    }
}
