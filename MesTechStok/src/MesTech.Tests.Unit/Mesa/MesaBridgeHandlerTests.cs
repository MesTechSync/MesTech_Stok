using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// Bridge handler runtime testleri — DEV 6 Dalga 2 gorev 6.05.
/// Mock IPublishEndpoint + Mock ITenantProvider ile pipeline dogrulama.
/// RabbitMQ Docker bagimli degil — tamamen unit test.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
public class MesaBridgeHandlerTests
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Mock<ITenantProvider> CreateTenantProvider()
    {
        var mock = new Mock<ITenantProvider>();
        mock.Setup(x => x.GetCurrentTenantId()).Returns(TestTenantId);
        return mock;
    }

    private static Mock<IMesaEventMonitor> CreateMonitor() => new();

    // ══════════════════════════════════════════════
    //  ProductCreatedBridgeHandler
    // ══════════════════════════════════════════════

    [Fact]
    public async Task ProductCreated_ShouldPublishMesaEvent_WithCorrectFields()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new ProductCreatedBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<ProductCreatedBridgeHandler>>().Object);

        var domainEvent = new ProductCreatedEvent(
            Guid.NewGuid(), "SKU-TEST-001", "Test Urun", 199.99m, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<ProductCreatedEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishProductCreatedAsync(
                It.Is<MesaProductCreatedEvent>(e =>
                    e.SKU == "SKU-TEST-001" &&
                    e.Name == "Test Urun" &&
                    e.SalePrice == 199.99m &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  LowStockBridgeHandler
    // ══════════════════════════════════════════════

    [Fact]
    public async Task LowStockDetected_ShouldPublishMesaStockLowEvent()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new LowStockBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<LowStockBridgeHandler>>().Object);

        var domainEvent = new LowStockDetectedEvent(
            Guid.NewGuid(), "SKU-LOW-001", 3, 10, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<LowStockDetectedEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishStockLowAsync(
                It.Is<MesaStockLowEvent>(e =>
                    e.SKU == "SKU-LOW-001" &&
                    e.CurrentStock == 3 &&
                    e.MinimumStock == 10 &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  OrderPlacedBridgeHandler
    // ══════════════════════════════════════════════

    [Fact]
    public async Task OrderPlaced_ShouldPublishMesaOrderEvent_WithOrderNumber()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new OrderPlacedBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<OrderPlacedBridgeHandler>>().Object);

        var domainEvent = new OrderPlacedEvent(
            Guid.NewGuid(), "ORD-2026-001", Guid.NewGuid(), 549.90m, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<OrderPlacedEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishOrderReceivedAsync(
                It.Is<MesaOrderReceivedEvent>(e =>
                    e.PlatformCode == "MesTech" &&
                    e.PlatformOrderId == "ORD-2026-001" &&
                    e.TotalAmount == 549.90m &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  PriceChangedBridgeHandler
    // ══════════════════════════════════════════════

    [Fact]
    public async Task PriceChanged_ShouldPublishMesaEvent_WithOldAndNewPrice()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new PriceChangedBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<PriceChangedBridgeHandler>>().Object);

        var domainEvent = new PriceChangedEvent(
            Guid.NewGuid(), "SKU-PRICE-001", 100m, 89.90m, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<PriceChangedEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishPriceChangedAsync(
                It.Is<MesaPriceChangedEvent>(e =>
                    e.SKU == "SKU-PRICE-001" &&
                    e.OldPrice == 100m &&
                    e.NewPrice == 89.90m &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  DomainEventNotification Wrapper
    // ══════════════════════════════════════════════

    [Fact]
    public void DomainEventNotification_ShouldWrapAndExposeEvent()
    {
        var domainEvent = new ProductCreatedEvent(
            Guid.NewGuid(), "SKU-WRAP", "Wrapper Test", 50m, DateTime.UtcNow);

        var notification = new DomainEventNotification<ProductCreatedEvent>(domainEvent);

        notification.DomainEvent.Should().BeSameAs(domainEvent);
        notification.DomainEvent.SKU.Should().Be("SKU-WRAP");
    }

    [Fact]
    public void DomainEventNotification_ShouldThrow_WhenNullEvent()
    {
        var act = () => new DomainEventNotification<ProductCreatedEvent>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("domainEvent");
    }

    // ══════════════════════════════════════════════
    //  TenantId Isolation
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BridgeHandlers_ShouldIncludeCorrectTenantId()
    {
        var tenantId = Guid.NewGuid();
        var tenantMock = new Mock<ITenantProvider>();
        tenantMock.Setup(x => x.GetCurrentTenantId()).Returns(tenantId);

        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new ProductCreatedBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            tenantMock.Object,
            new Mock<ILogger<ProductCreatedBridgeHandler>>().Object);

        var domainEvent = new ProductCreatedEvent(
            Guid.NewGuid(), "SKU-TENANT", "Tenant Test", 10m, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<ProductCreatedEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishProductCreatedAsync(
                It.Is<MesaProductCreatedEvent>(e => e.TenantId == tenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
