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
}
