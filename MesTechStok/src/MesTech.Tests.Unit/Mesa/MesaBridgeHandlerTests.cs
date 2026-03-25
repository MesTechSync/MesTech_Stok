using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
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
            Guid.NewGuid(), Guid.NewGuid(), "SKU-TEST-001", "Test Urun", 199.99m, DateTime.UtcNow);

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
            Guid.NewGuid(), Guid.NewGuid(), "SKU-LOW-001", 3, 10, DateTime.UtcNow);

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
            Guid.NewGuid(), Guid.NewGuid(), "ORD-2026-001", Guid.NewGuid(), 549.90m, DateTime.UtcNow);

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
            Guid.NewGuid(), Guid.NewGuid(), "SKU-PRICE-001", 100m, 89.90m, DateTime.UtcNow);

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
            Guid.NewGuid(), Guid.NewGuid(), "SKU-WRAP", "Wrapper Test", 50m, DateTime.UtcNow);

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
            Guid.NewGuid(), Guid.NewGuid(), "SKU-TENANT", "Tenant Test", 10m, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<ProductCreatedEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishProductCreatedAsync(
                It.Is<MesaProductCreatedEvent>(e => e.TenantId == tenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  InvoiceGeneratedBridgeHandler (Dalga 4)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task InvoiceSent_ShouldPublishMesaInvoiceGeneratedEvent()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new InvoiceGeneratedBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<InvoiceGeneratedBridgeHandler>>().Object);

        var domainEvent = new InvoiceSentEvent(
            Guid.NewGuid(), Guid.NewGuid(), "GIB-2026-001", "https://example.com/invoice.pdf", DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<InvoiceSentEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishInvoiceGeneratedAsync(
                It.Is<MesaInvoiceGeneratedEvent>(e =>
                    e.InvoiceId == domainEvent.InvoiceId &&
                    e.PdfUrl == "https://example.com/invoice.pdf" &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  InvoiceCancelledBridgeHandler (Dalga 4)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task InvoiceCancelled_ShouldPublishMesaInvoiceCancelledEvent()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new InvoiceCancelledBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<InvoiceCancelledBridgeHandler>>().Object);

        var domainEvent = new InvoiceCancelledEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "FTR-2026-001", "Musteri talebi", DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<InvoiceCancelledEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishInvoiceCancelledAsync(
                It.Is<MesaInvoiceCancelledEvent>(e =>
                    e.InvoiceNumber == "FTR-2026-001" &&
                    e.CancelReason == "Musteri talebi" &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  ReturnCreatedBridgeHandler (Dalga 4)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task ReturnCreated_ShouldPublishMesaReturnCreatedEvent()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new ReturnCreatedBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<ReturnCreatedBridgeHandler>>().Object);

        var domainEvent = new ReturnCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PlatformType.Trendyol,
            ReturnReason.DefectiveProduct,
            DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<ReturnCreatedEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishReturnCreatedAsync(
                It.Is<MesaReturnCreatedEvent>(e =>
                    e.ReturnRequestId == domainEvent.ReturnRequestId &&
                    e.PlatformCode == "Trendyol" &&
                    e.ReturnReason == "DefectiveProduct" &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  ReturnResolvedBridgeHandler (Dalga 4)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task ReturnResolved_ShouldPublishMesaReturnResolvedEvent()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new ReturnResolvedBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<ReturnResolvedBridgeHandler>>().Object);

        var domainEvent = new ReturnResolvedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ReturnStatus.Refunded,
            149.90m, DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<ReturnResolvedEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishReturnResolvedAsync(
                It.Is<MesaReturnResolvedEvent>(e =>
                    e.Resolution == "Refunded" &&
                    e.RefundAmount == 149.90m &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  BuyboxLostBridgeHandler (Dalga 4)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BuyboxLost_ShouldPublishMesaBuyboxLostEvent()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new BuyboxLostBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<BuyboxLostBridgeHandler>>().Object);

        var domainEvent = new BuyboxLostEvent(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-BB-001", 149.90m, 139.90m, "TeknoMarket", DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<BuyboxLostEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishBuyboxLostAsync(
                It.Is<MesaBuyboxLostEvent>(e =>
                    e.SKU == "SKU-BB-001" &&
                    e.CompetitorPrice == 139.90m &&
                    e.CompetitorName == "TeknoMarket" &&
                    e.PriceDifference == 10m &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════
    //  SupplierFeedSyncedBridgeHandler (Dalga 4)
    // ══════════════════════════════════════════════

    [Fact]
    public async Task SupplierFeedSynced_ShouldPublishMesaSupplierFeedSyncedEvent()
    {
        var publisherMock = new Mock<IMesaEventPublisher>();
        var handler = new SupplierFeedSyncedBridgeHandler(
            publisherMock.Object,
            CreateMonitor().Object,
            CreateTenantProvider().Object,
            new Mock<ILogger<SupplierFeedSyncedBridgeHandler>>().Object);

        var domainEvent = new SupplierFeedSyncedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 150, 30, 5,
            FeedSyncStatus.Completed,
            DateTime.UtcNow);

        await handler.Handle(
            new DomainEventNotification<SupplierFeedSyncedEvent>(domainEvent),
            CancellationToken.None);

        publisherMock.Verify(
            x => x.PublishSupplierFeedSyncedAsync(
                It.Is<MesaSupplierFeedSyncedEvent>(e =>
                    e.SupplierId == domainEvent.SupplierId &&
                    e.ProductsTotal == 150 &&
                    e.ProductsUpdated == 30 &&
                    e.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
