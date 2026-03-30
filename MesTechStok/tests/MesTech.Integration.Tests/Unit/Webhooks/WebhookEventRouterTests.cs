using MediatR;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Webhooks;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Webhooks;

/// <summary>
/// WebhookEventRouter tests: alias normalization, ResolvePlatformType, domain event publishing.
/// G493: EventTypeAliases 35+ alias, ResolvePlatformType 16 platform.
/// </summary>
public class WebhookEventRouterTests
{
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<ILogger<WebhookEventRouter>> _loggerMock;
    private readonly WebhookEventRouter _router;

    public WebhookEventRouterTests()
    {
        _publisherMock = new Mock<IPublisher>();
        _loggerMock = new Mock<ILogger<WebhookEventRouter>>();
        _router = new WebhookEventRouter(_publisherMock.Object, _loggerMock.Object);
    }

    // ─── EventTypeAliases — Normalization ───

    [Theory]
    [InlineData("order.created", "order.created")]
    [InlineData("order_created", "order.created")]
    [InlineData("orders/create", "order.created")]           // Shopify
    [InlineData("orders/paid", "order.created")]              // Shopify
    [InlineData("order.new", "order.created")]
    [InlineData("ITEM.SOLD", "order.created")]                // eBay
    [InlineData("order_status_changed", "order.created")]     // Ozon
    public async Task RouteAsync_OrderCreatedAliases_PublishOrderReceivedEvent(string alias, string expected)
    {
        // Arrange
        var payload = """{"orderId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","platformOrderId":"ORD-123","totalAmount":99.99}""";

        // Act
        var result = await _router.RouteAsync("trendyol", alias, payload, CancellationToken.None);

        // Assert
        result.Should().Be(expected);
        _publisherMock.Verify(p => p.Publish(
            It.Is<DomainEventNotification<OrderReceivedEvent>>(n =>
                n.DomainEvent.PlatformOrderId == "ORD-123"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("order.cancelled")]
    [InlineData("order_cancelled")]
    [InlineData("orders/cancelled")]     // Shopify
    [InlineData("order.cancel")]
    public async Task RouteAsync_OrderCancelledAliases_PublishOrderCancelledEvent(string alias)
    {
        // Arrange
        var payload = """{"orderId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","platformOrderId":"ORD-456","reason":"customer request"}""";

        // Act
        var result = await _router.RouteAsync("shopify", alias, payload, CancellationToken.None);

        // Assert
        result.Should().Be("order.cancelled");
        _publisherMock.Verify(p => p.Publish(
            It.Is<DomainEventNotification<OrderCancelledEvent>>(n =>
                n.DomainEvent.Reason == "customer request"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("order.shipped")]
    [InlineData("order_shipped")]
    [InlineData("orders/fulfilled")]           // Shopify
    [InlineData("ORDER.DELIVERY_UPDATE")]      // eBay
    public async Task RouteAsync_OrderShippedAliases_PublishOrderShippedEvent(string alias)
    {
        // Arrange
        var payload = """{"orderId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","trackingNumber":"TRK-789","carrier":"Yurtici"}""";

        // Act
        var result = await _router.RouteAsync("ebay", alias, payload, CancellationToken.None);

        // Assert
        result.Should().Be("order.shipped");
        _publisherMock.Verify(p => p.Publish(
            It.Is<DomainEventNotification<OrderShippedEvent>>(n =>
                n.DomainEvent.TrackingNumber == "TRK-789"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("stock.updated")]
    [InlineData("stock_updated")]
    [InlineData("stock_changed")]                // Ozon
    [InlineData("inventory_levels/update")]      // Shopify
    [InlineData("item_price_changed")]           // Ozon
    public async Task RouteAsync_StockUpdatedAliases_PublishStockChangedEvent(string alias)
    {
        // Arrange
        var payload = """{"productId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","sku":"SKU-100","quantity":50}""";

        // Act
        var result = await _router.RouteAsync("ozon", alias, payload, CancellationToken.None);

        // Assert
        result.Should().Be("stock.updated");
        _publisherMock.Verify(p => p.Publish(
            It.Is<DomainEventNotification<StockChangedEvent>>(n =>
                n.DomainEvent.Sku == "SKU-100" &&
                n.DomainEvent.NewQuantity == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("product.created")]
    [InlineData("product_created")]
    [InlineData("products/create")]      // Shopify
    public async Task RouteAsync_ProductCreatedAliases_PublishProductCreatedEvent(string alias)
    {
        // Arrange
        var payload = """{"productId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","sku":"NEW-SKU","name":"Test Product","salePrice":149.90}""";

        // Act
        var result = await _router.RouteAsync("shopify", alias, payload, CancellationToken.None);

        // Assert
        result.Should().Be("product.created");
        _publisherMock.Verify(p => p.Publish(
            It.Is<DomainEventNotification<ProductCreatedEvent>>(n =>
                n.DomainEvent.Sku == "NEW-SKU" &&
                n.DomainEvent.Name == "Test Product"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("return.created")]
    [InlineData("return_created")]
    [InlineData("refunds/create")]       // Shopify
    [InlineData("return.requested")]
    [InlineData("claim.created")]
    public async Task RouteAsync_ReturnCreatedAliases_PublishReturnCreatedEvent(string alias)
    {
        // Arrange
        var payload = """{"returnId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","orderId":"00000000-0000-0000-0000-000000000003"}""";

        // Act
        var result = await _router.RouteAsync("hepsiburada", alias, payload, CancellationToken.None);

        // Assert
        result.Should().Be("return.created");
        _publisherMock.Verify(p => p.Publish(
            It.IsAny<DomainEventNotification<ReturnCreatedEvent>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("invoice.created")]
    [InlineData("invoice_created")]
    public async Task RouteAsync_InvoiceCreatedAliases_PublishInvoiceCreatedEvent(string alias)
    {
        // Arrange
        var payload = """{"invoiceId":"00000000-0000-0000-0000-000000000001","orderId":"00000000-0000-0000-0000-000000000002","tenantId":"00000000-0000-0000-0000-000000000003","grandTotal":250.00}""";

        // Act
        var result = await _router.RouteAsync("trendyol", alias, payload, CancellationToken.None);

        // Assert
        result.Should().Be("invoice.created");
        _publisherMock.Verify(p => p.Publish(
            It.IsAny<DomainEventNotification<InvoiceCreatedEvent>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Unknown Event Type ───

    [Fact]
    public async Task RouteAsync_UnknownEventType_ReturnsNull()
    {
        // Act
        var result = await _router.RouteAsync("trendyol", "some.unknown.event", "{}", CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _publisherMock.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Invalid JSON ───

    [Fact]
    public async Task RouteAsync_InvalidJsonPayload_ReturnsNull()
    {
        // Act
        var result = await _router.RouteAsync("trendyol", "order.created", "not-valid-json", CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _publisherMock.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── ResolvePlatformType ───

    [Fact]
    public async Task RouteAsync_ReturnCreated_ResolvesCorrectPlatformType()
    {
        // Arrange — return.created uses ResolvePlatformType
        var payload = """{"returnId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","orderId":"00000000-0000-0000-0000-000000000003"}""";

        // Act — platform "hepsiburada" should resolve to PlatformType.Hepsiburada
        var result = await _router.RouteAsync("hepsiburada", "return.created", payload, CancellationToken.None);

        // Assert
        result.Should().Be("return.created");
        _publisherMock.Verify(p => p.Publish(
            It.Is<DomainEventNotification<ReturnCreatedEvent>>(n =>
                n.DomainEvent.PlatformType == PlatformType.Hepsiburada),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Fallback Field Extraction ───

    [Fact]
    public async Task RouteAsync_OrderCreated_FallsBackToIdField()
    {
        // Arrange — no platformOrderId, should fall back to "id"
        var payload = """{"orderId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","id":"FALLBACK-ID","totalAmount":50}""";

        // Act
        var result = await _router.RouteAsync("trendyol", "order.created", payload, CancellationToken.None);

        // Assert
        _publisherMock.Verify(p => p.Publish(
            It.Is<DomainEventNotification<OrderReceivedEvent>>(n =>
                n.DomainEvent.PlatformOrderId == "FALLBACK-ID"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteAsync_OrderCreated_FallsBackToTotalPriceField()
    {
        // Arrange — no totalAmount, should fall back to "total_price"
        var payload = """{"orderId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","platformOrderId":"ORD-1","total_price":75.50}""";

        // Act
        var result = await _router.RouteAsync("shopify", "order.created", payload, CancellationToken.None);

        // Assert
        _publisherMock.Verify(p => p.Publish(
            It.Is<DomainEventNotification<OrderReceivedEvent>>(n =>
                n.DomainEvent.TotalAmount == 75.50m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Case-Insensitive Alias Lookup ───

    [Theory]
    [InlineData("ORDER.CREATED")]
    [InlineData("Order.Created")]
    [InlineData("ORDER.created")]
    public async Task RouteAsync_CaseInsensitiveAliasLookup(string alias)
    {
        // Arrange
        var payload = """{"orderId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","platformOrderId":"ORD-1","totalAmount":10}""";

        // Act
        var result = await _router.RouteAsync("trendyol", alias, payload, CancellationToken.None);

        // Assert
        result.Should().Be("order.created");
    }

    // ─── Missing Fields Default to Safe Values ───

    [Fact]
    public async Task RouteAsync_OrderCreated_MissingFields_UsesDefaults()
    {
        // Arrange — empty JSON, all fields missing
        var payload = """{}""";

        // Act
        var result = await _router.RouteAsync("trendyol", "order.created", payload, CancellationToken.None);

        // Assert — should still succeed with default values
        result.Should().Be("order.created");
        _publisherMock.Verify(p => p.Publish(
            It.Is<DomainEventNotification<OrderReceivedEvent>>(n =>
                n.DomainEvent.PlatformOrderId == "unknown" &&
                n.DomainEvent.TotalAmount == 0m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteAsync_StockUpdated_MissingQuantity_DefaultsToZero()
    {
        // Arrange
        var payload = """{"productId":"00000000-0000-0000-0000-000000000001","tenantId":"00000000-0000-0000-0000-000000000002","sku":"SKU-1"}""";

        // Act
        var result = await _router.RouteAsync("ozon", "stock.updated", payload, CancellationToken.None);

        // Assert
        _publisherMock.Verify(p => p.Publish(
            It.Is<DomainEventNotification<StockChangedEvent>>(n =>
                n.DomainEvent.NewQuantity == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
