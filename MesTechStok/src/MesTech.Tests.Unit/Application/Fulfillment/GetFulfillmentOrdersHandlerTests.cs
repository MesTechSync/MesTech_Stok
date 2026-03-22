using FluentAssertions;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Fulfillment;

[Trait("Category", "Unit")]
[Trait("Domain", "Fulfillment")]
public class GetFulfillmentOrdersHandlerTests
{
    private readonly Mock<IFulfillmentProviderFactory> _factory = new();
    private readonly Mock<IFulfillmentProvider> _provider = new();
    private readonly Mock<ILogger<GetFulfillmentOrdersHandler>> _logger = new();

    private GetFulfillmentOrdersHandler CreateSut() => new(_factory.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsFulfillmentOrders()
    {
        // Arrange
        var since = DateTime.UtcNow.AddDays(-7);
        var query = new GetFulfillmentOrdersQuery(FulfillmentCenter.AmazonFBA, since);

        var orders = new List<FulfillmentOrderResult>
        {
            new("ORD-001", "Shipped",
                new List<FulfillmentOrderItem> { new("SKU-001", 2, 2) },
                DateTime.UtcNow.AddDays(-1), "TRK-001", "AmazonLogistics"),
            new("ORD-002", "Processing",
                new List<FulfillmentOrderItem> { new("SKU-002", 1, 0) })
        };

        _factory.Setup(f => f.Resolve(FulfillmentCenter.AmazonFBA)).Returns(_provider.Object);
        _provider
            .Setup(p => p.GetFulfillmentOrdersAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.AsReadOnly());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].OrderId.Should().Be("ORD-001");
        result[0].Status.Should().Be("Shipped");
        result[0].TrackingNumber.Should().Be("TRK-001");
    }

    [Fact]
    public async Task Handle_ProviderNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var query = new GetFulfillmentOrdersQuery(FulfillmentCenter.Hepsilojistik, DateTime.UtcNow.AddDays(-7));

        _factory.Setup(f => f.Resolve(FulfillmentCenter.Hepsilojistik))
            .Returns((IFulfillmentProvider?)null);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Hepsilojistik*");
    }

    [Fact]
    public async Task Handle_NoOrdersSince_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetFulfillmentOrdersQuery(FulfillmentCenter.AmazonFBA, DateTime.UtcNow);

        _factory.Setup(f => f.Resolve(FulfillmentCenter.AmazonFBA)).Returns(_provider.Object);
        _provider
            .Setup(p => p.GetFulfillmentOrdersAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FulfillmentOrderResult>().AsReadOnly());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
