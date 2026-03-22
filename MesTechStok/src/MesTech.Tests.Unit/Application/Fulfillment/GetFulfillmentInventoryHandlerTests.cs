using FluentAssertions;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Fulfillment;

[Trait("Category", "Unit")]
[Trait("Domain", "Fulfillment")]
public class GetFulfillmentInventoryHandlerTests
{
    private readonly Mock<IFulfillmentProviderFactory> _factory = new();
    private readonly Mock<IFulfillmentProvider> _provider = new();
    private readonly Mock<ILogger<GetFulfillmentInventoryHandler>> _logger = new();

    private GetFulfillmentInventoryHandler CreateSut() => new(_factory.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsInventory()
    {
        // Arrange
        var skus = new List<string> { "SKU-001", "SKU-002" };
        var query = new GetFulfillmentInventoryQuery(FulfillmentCenter.AmazonFBA, skus);

        var inventory = new FulfillmentInventory(
            FulfillmentCenter.AmazonFBA,
            new List<FulfillmentStock>
            {
                new("SKU-001", 50, 5, 10),
                new("SKU-002", 30, 2, 0)
            },
            DateTime.UtcNow);

        _factory.Setup(f => f.Resolve(FulfillmentCenter.AmazonFBA)).Returns(_provider.Object);
        _provider
            .Setup(p => p.GetInventoryLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Center.Should().Be(FulfillmentCenter.AmazonFBA);
        result.Stocks.Should().HaveCount(2);
        result.Stocks[0].SKU.Should().Be("SKU-001");
        result.Stocks[0].AvailableQuantity.Should().Be(50);
    }

    [Fact]
    public async Task Handle_ProviderNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var query = new GetFulfillmentInventoryQuery(
            FulfillmentCenter.TrendyolFulfillment,
            new List<string> { "SKU-001" });

        _factory.Setup(f => f.Resolve(FulfillmentCenter.TrendyolFulfillment))
            .Returns((IFulfillmentProvider?)null);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TrendyolFulfillment*");
    }

    [Fact]
    public async Task Handle_EmptySkuList_StillCallsProvider()
    {
        // Arrange
        var query = new GetFulfillmentInventoryQuery(
            FulfillmentCenter.AmazonFBA,
            new List<string>());

        var inventory = new FulfillmentInventory(
            FulfillmentCenter.AmazonFBA,
            new List<FulfillmentStock>(),
            DateTime.UtcNow);

        _factory.Setup(f => f.Resolve(FulfillmentCenter.AmazonFBA)).Returns(_provider.Object);
        _provider
            .Setup(p => p.GetInventoryLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Stocks.Should().BeEmpty();
        _provider.Verify(
            p => p.GetInventoryLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
