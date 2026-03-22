using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Tests.Unit._Shared;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// Dalga 3 Order entity tests — cargo fields, platform integration, domain events.
/// Supplements OrderTests.cs without duplication.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga3")]
public class OrderDalga3Tests
{
    // ── MarkAsShipped from invalid statuses ──

    [Fact]
    public void MarkAsShipped_FromShipped_ThrowsInvalidOperationException()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Shipped);

        var act = () => order.MarkAsShipped("TR123", CargoProvider.MngKargo);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot ship order in Shipped status*");
    }

    [Fact]
    public void MarkAsShipped_FromCancelled_ThrowsInvalidOperationException()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Cancelled);

        var act = () => order.MarkAsShipped("TR456", CargoProvider.PttKargo);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot ship order in Cancelled status*");
    }

    // ── OrderShippedEvent content validation ──

    [Fact]
    public void MarkAsShipped_RaisedEvent_ContainsCorrectFields()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Confirmed);

        order.MarkAsShipped("TRACK-ABC-789", CargoProvider.Hepsijet);

        var evt = order.DomainEvents.OfType<OrderShippedEvent>().Single();
        evt.OrderId.Should().Be(order.Id);
        evt.TrackingNumber.Should().Be("TRACK-ABC-789");
        evt.CargoProvider.Should().Be(CargoProvider.Hepsijet);
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsShipped_SetsAllCargoFields()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Confirmed);

        order.MarkAsShipped("YK-2026-001", CargoProvider.YurticiKargo);

        order.TrackingNumber.Should().Be("YK-2026-001");
        order.CargoProvider.Should().Be(CargoProvider.YurticiKargo);
        order.ShippedAt.Should().NotBeNull();
        order.ShippedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    // ── SourcePlatform enum coverage ──

    [Theory]
    [InlineData(PlatformType.Trendyol)]
    [InlineData(PlatformType.Hepsiburada)]
    [InlineData(PlatformType.Ciceksepeti)]
    [InlineData(PlatformType.OpenCart)]
    [InlineData(PlatformType.N11)]
    [InlineData(PlatformType.Amazon)]
    [InlineData(PlatformType.Etsy)]
    public void SourcePlatform_CanBeSetToAllPlatformTypes(PlatformType platform)
    {
        var order = FakeData.CreateOrder(sourcePlatform: platform);

        order.SourcePlatform.Should().Be(platform);
    }

    // ── AutoShipment fields ──

    [Fact]
    public void AutoShipmentEnabled_DefaultsFalse()
    {
        var order = new Order();

        order.AutoShipmentEnabled.Should().BeFalse();
        order.AutoShipmentScheduledAt.Should().BeNull();
    }

    [Fact]
    public void AutoShipmentEnabled_CanBeSetToTrue()
    {
        var order = FakeData.CreateOrder();
        var scheduledAt = DateTime.UtcNow.AddHours(2);

        order.AutoShipmentEnabled = true;
        order.ScheduleAutoShipment(scheduledAt);

        order.AutoShipmentEnabled.Should().BeTrue();
        order.AutoShipmentScheduledAt.Should().Be(scheduledAt);
    }

    // ── External platform fields ──

    [Fact]
    public void ExternalOrderId_CanBeSetForPlatformOrders()
    {
        var order = FakeData.CreateOrder(sourcePlatform: PlatformType.Trendyol);

        order.ExternalOrderId = "TY-98765432";
        order.PlatformOrderNumber = "2026-TR-001";

        order.ExternalOrderId.Should().Be("TY-98765432");
        order.PlatformOrderNumber.Should().Be("2026-TR-001");
    }

    [Fact]
    public void ExternalOrderId_DefaultsNull()
    {
        var order = new Order();

        order.ExternalOrderId.Should().BeNull();
        order.PlatformOrderNumber.Should().BeNull();
    }

    // ── Cargo barcode and delivery ──

    [Fact]
    public void CargoBarcode_CanBeSetIndependently()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Confirmed);
        order.MarkAsShipped("SR-111", CargoProvider.SuratKargo);

        order.SetCargoBarcode("BC-SURAT-2026-001");

        order.CargoBarcode.Should().Be("BC-SURAT-2026-001");
        order.TrackingNumber.Should().Be("SR-111");
    }

    [Fact]
    public void DeliveredAt_CanBeSetAfterShipment()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Confirmed);
        order.MarkAsShipped("AR-222", CargoProvider.ArasKargo);

        order.MarkAsDelivered();

        order.DeliveredAt.Should().NotBeNull();
        order.DeliveredAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        order.Status.Should().Be(OrderStatus.Delivered);
        order.ShippedAt.Should().NotBeNull();
    }

    // ── RowVersion concurrency ──

    [Fact]
    public void RowVersion_DefaultsNull()
    {
        var order = new Order();

        order.RowVersion.Should().BeNull();
    }

    [Fact]
    public void RowVersion_CanBeAssigned()
    {
        var order = FakeData.CreateOrder();
        var version = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        order.RowVersion = version;

        order.RowVersion.Should().BeEquivalentTo(version);
    }

    // ── FakeData helper validation ──

    [Fact]
    public void FakeData_CreateOrder_ProducesValidOrder()
    {
        var order = FakeData.CreateOrder(
            customerId: Guid.NewGuid(),
            status: OrderStatus.Confirmed,
            sourcePlatform: PlatformType.Hepsiburada);

        order.OrderNumber.Should().StartWith("ORD-");
        order.CustomerId.Should().NotBeEmpty();
        order.TenantId.Should().NotBeEmpty();
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.SourcePlatform.Should().Be(PlatformType.Hepsiburada);
        order.SubTotal.Should().BeGreaterThan(0);
    }

    // ── Multiple cargo providers via Theory ──

    [Theory]
    [InlineData(CargoProvider.YurticiKargo)]
    [InlineData(CargoProvider.ArasKargo)]
    [InlineData(CargoProvider.SuratKargo)]
    [InlineData(CargoProvider.MngKargo)]
    [InlineData(CargoProvider.PttKargo)]
    [InlineData(CargoProvider.Hepsijet)]
    [InlineData(CargoProvider.UPS)]
    public void MarkAsShipped_AcceptsAllCargoProviders(CargoProvider provider)
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Confirmed);

        order.MarkAsShipped($"TRACK-{provider}", provider);

        order.CargoProvider.Should().Be(provider);
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    // ── Domain event count isolation ──

    [Fact]
    public void MarkAsShipped_DoesNotDuplicateEvents_WhenCalledOnce()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Confirmed);

        order.MarkAsShipped("SINGLE-001", CargoProvider.MngKargo);

        order.DomainEvents.Should().HaveCount(1);
        order.DomainEvents.First().Should().BeOfType<OrderShippedEvent>();
    }

    [Fact]
    public void ClearDomainEvents_RemovesShippedEvent()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Confirmed);
        order.MarkAsShipped("CLR-001", CargoProvider.UPS);
        order.DomainEvents.Should().HaveCount(1);

        order.ClearDomainEvents();

        order.DomainEvents.Should().BeEmpty();
    }
}
