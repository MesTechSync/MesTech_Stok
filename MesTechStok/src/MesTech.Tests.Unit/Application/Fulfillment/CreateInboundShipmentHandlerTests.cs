using FluentAssertions;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Fulfillment.Commands.CreateInboundShipment;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Fulfillment;

[Trait("Category", "Unit")]
[Trait("Domain", "Fulfillment")]
public class CreateInboundShipmentHandlerTests
{
    private readonly Mock<IFulfillmentProviderFactory> _factory = new();
    private readonly Mock<IFulfillmentProvider> _provider = new();
    private readonly Mock<ILogger<CreateInboundShipmentHandler>> _logger = new();

    private CreateInboundShipmentHandler CreateSut() => new(_factory.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var items = new List<InboundItem> { new("SKU-001", 10) };
        var command = new CreateInboundShipmentCommand(
            FulfillmentCenter.AmazonFBA, "TestShipment", items, DateTime.UtcNow.AddDays(3));

        var expected = new InboundResult(true, "SHIP-123", null, DateTime.UtcNow.AddDays(5));

        _factory.Setup(f => f.Resolve(FulfillmentCenter.AmazonFBA)).Returns(_provider.Object);
        _provider
            .Setup(p => p.CreateInboundShipmentAsync(It.IsAny<InboundShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ShipmentId.Should().Be("SHIP-123");
        _provider.Verify(p => p.CreateInboundShipmentAsync(
            It.Is<InboundShipmentRequest>(r =>
                r.ShipmentName == "TestShipment" &&
                r.DestinationCenter == FulfillmentCenter.AmazonFBA &&
                r.Items.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProviderNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var items = new List<InboundItem> { new("SKU-001", 5) };
        var command = new CreateInboundShipmentCommand(
            FulfillmentCenter.Hepsilojistik, "TestShipment", items);

        _factory.Setup(f => f.Resolve(FulfillmentCenter.Hepsilojistik)).Returns((IFulfillmentProvider?)null);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Hepsilojistik*");
    }

    [Fact]
    public async Task Handle_ProviderReturnsFailure_ReturnsFailedResult()
    {
        // Arrange
        var items = new List<InboundItem> { new("SKU-002", 20) };
        var command = new CreateInboundShipmentCommand(
            FulfillmentCenter.AmazonFBA, "FailShipment", items);

        var failResult = new InboundResult(false, string.Empty, "Warehouse capacity exceeded");

        _factory.Setup(f => f.Resolve(FulfillmentCenter.AmazonFBA)).Returns(_provider.Object);
        _provider
            .Setup(p => p.CreateInboundShipmentAsync(It.IsAny<InboundShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failResult);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Warehouse capacity exceeded");
    }
}
