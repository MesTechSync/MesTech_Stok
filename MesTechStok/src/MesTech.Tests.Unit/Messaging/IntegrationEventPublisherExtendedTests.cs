using MassTransit;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MesTech.Tests.Unit.Messaging;

public class IntegrationEventPublisherExtendedTests
{
    private readonly Mock<IPublishEndpoint> _mockPublish = new();
    private readonly Mock<ITenantProvider> _mockTenant = new();
    private readonly IntegrationEventPublisher _publisher;

    public IntegrationEventPublisherExtendedTests()
    {
        _mockTenant.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        _publisher = new IntegrationEventPublisher(
            _mockPublish.Object,
            _mockTenant.Object,
            NullLogger<IntegrationEventPublisher>.Instance);
    }

    [Fact]
    public async Task PublishOrderShippedAsync_PublishesWithTenantId()
    {
        var orderId = Guid.NewGuid();
        await _publisher.PublishOrderShippedAsync(orderId, "TR123456", "YurticiKargo");

        _mockPublish.Verify(p => p.Publish(
            It.Is<OrderShippedIntegrationEvent>(e =>
                e.OrderId == orderId &&
                e.TrackingNumber == "TR123456" &&
                e.CargoProvider == "YurticiKargo" &&
                e.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishProductUpdatedAsync_PublishesWithTenantId()
    {
        var productId = Guid.NewGuid();
        await _publisher.PublishProductUpdatedAsync(productId, "SKU-001", "Price");

        _mockPublish.Verify(p => p.Publish(
            It.Is<ProductUpdatedIntegrationEvent>(e =>
                e.ProductId == productId &&
                e.SKU == "SKU-001" &&
                e.UpdatedField == "Price" &&
                e.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ TUR 3-4 Chain Event Publisher Tests (G518) ═══

    [Fact]
    public async Task PublishShipmentCostRecordedAsync_PublishesZ7Event()
    {
        var orderId = Guid.NewGuid();
        await _publisher.PublishShipmentCostRecordedAsync(orderId, "TR999", "ArasKargo", 45.50m);

        _mockPublish.Verify(p => p.Publish(
            It.Is<ShipmentCostRecordedIntegrationEvent>(e =>
                e.OrderId == orderId &&
                e.TrackingNumber == "TR999" &&
                e.CargoProvider == "ArasKargo" &&
                e.ShippingCost == 45.50m &&
                e.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishZeroStockDetectedAsync_PublishesZ8Event()
    {
        var productId = Guid.NewGuid();
        await _publisher.PublishZeroStockDetectedAsync(productId, "SKU-ZERO", 5);

        _mockPublish.Verify(p => p.Publish(
            It.Is<ZeroStockIntegrationEvent>(e =>
                e.ProductId == productId &&
                e.SKU == "SKU-ZERO" &&
                e.PreviousStock == 5 &&
                e.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishStaleOrderDetectedAsync_PublishesZ11Event()
    {
        var orderId = Guid.NewGuid();
        await _publisher.PublishStaleOrderDetectedAsync(orderId, "ORD-001", "Trendyol", 52.5);

        _mockPublish.Verify(p => p.Publish(
            It.Is<StaleOrderDetectedIntegrationEvent>(e =>
                e.OrderId == orderId &&
                e.OrderNumber == "ORD-001" &&
                e.PlatformCode == "Trendyol" &&
                e.HoursElapsed == 52.5 &&
                e.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishPlatformDeactivatedAsync_PublishesEvent()
    {
        var productId = Guid.NewGuid();
        await _publisher.PublishPlatformDeactivatedAsync(productId, "SKU-OFF", "HepsiBurada");

        _mockPublish.Verify(p => p.Publish(
            It.Is<PlatformDeactivatedIntegrationEvent>(e =>
                e.ProductId == productId &&
                e.SKU == "SKU-OFF" &&
                e.PlatformCode == "HepsiBurada" &&
                e.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ TUR 4 Dalga9 Event Publisher Tests (G521) ═══

    [Fact]
    public async Task PublishEInvoiceSentAsync_PublishesDalga9Event()
    {
        var invoiceId = Guid.NewGuid();
        await _publisher.PublishEInvoiceSentAsync(invoiceId, "GIB2026000001", "Sovos", 1180.00m, "TRY");

        _mockPublish.Verify(p => p.Publish(
            It.Is<EInvoiceSentIntegrationEvent>(e =>
                e.InvoiceId == invoiceId &&
                e.EttnNo == "GIB2026000001" &&
                e.ProviderId == "Sovos" &&
                e.TotalAmount == 1180.00m &&
                e.Currency == "TRY" &&
                e.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishEInvoiceCancelledAsync_PublishesDalga9Event()
    {
        var invoiceId = Guid.NewGuid();
        await _publisher.PublishEInvoiceCancelledAsync(invoiceId, "GIB2026000002", "Musteri talebi");

        _mockPublish.Verify(p => p.Publish(
            It.Is<EInvoiceCancelledIntegrationEvent>(e =>
                e.InvoiceId == invoiceId &&
                e.EttnNo == "GIB2026000002" &&
                e.Reason == "Musteri talebi" &&
                e.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishErpSyncCompletedAsync_PublishesDalga9Event()
    {
        var entityId = Guid.NewGuid();
        await _publisher.PublishErpSyncCompletedAsync("Parasut", "Invoice", entityId, "PS-12345", true);

        _mockPublish.Verify(p => p.Publish(
            It.Is<ErpSyncCompletedIntegrationEvent>(e =>
                e.ErpProvider == "Parasut" &&
                e.EntityType == "Invoice" &&
                e.EntityId == entityId &&
                e.ErpRef == "PS-12345" &&
                e.Success == true &&
                e.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishEbayOrderReceivedAsync_PublishesDalga9Event()
    {
        await _publisher.PublishEbayOrderReceivedAsync("EBAY-12345", "buyer_uk", 89.99m, "GBP");

        _mockPublish.Verify(p => p.Publish(
            It.Is<EbayOrderReceivedIntegrationEvent>(e =>
                e.EbayOrderId == "EBAY-12345" &&
                e.BuyerUsername == "buyer_uk" &&
                e.TotalAmount == 89.99m &&
                e.Currency == "GBP" &&
                e.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishCreditBalanceLowAsync_PublishesDalga9Event()
    {
        await _publisher.PublishCreditBalanceLowAsync("Sovos", 15, 50);

        _mockPublish.Verify(p => p.Publish(
            It.Is<CreditBalanceLowIntegrationEvent>(e =>
                e.ProviderId == "Sovos" &&
                e.RemainingCredits == 15 &&
                e.ThresholdCredits == 50 &&
                e.TenantId != Guid.Empty),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
