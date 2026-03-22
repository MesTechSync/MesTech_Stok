using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.DTOs.Shipping;
using MesTech.Application.Features.Shipping.Queries.GetShipmentStatus;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Moq;

namespace MesTech.Tests.Unit.Application.Shipping;

[Trait("Category", "Unit")]
[Trait("Domain", "Shipments")]
public class GetShipmentStatusHandlerTests
{
    private readonly Mock<ICargoProviderFactory> _cargoFactory = new();
    private readonly Mock<ICargoAdapter> _cargoAdapter = new();

    private GetShipmentStatusHandler CreateSut() => new(_cargoFactory.Object);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsShipmentStatus()
    {
        // Arrange
        var query = new GetShipmentStatusQuery(Guid.NewGuid(), "TRK-12345", CargoProvider.YurticiKargo);

        var trackingResult = new TrackingResult
        {
            TrackingNumber = "TRK-12345",
            Status = CargoStatus.InTransit,
            EstimatedDelivery = DateTime.UtcNow.AddDays(2),
            Events = new List<TrackingEvent>
            {
                new()
                {
                    Timestamp = DateTime.UtcNow.AddHours(-5),
                    Location = "Istanbul",
                    Description = "Kargo dagitim merkezinde",
                    Status = CargoStatus.InTransit
                }
            }
        };

        _cargoFactory.Setup(f => f.Resolve(CargoProvider.YurticiKargo)).Returns(_cargoAdapter.Object);
        _cargoAdapter
            .Setup(a => a.TrackShipmentAsync("TRK-12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackingResult);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TrackingNumber.Should().Be("TRK-12345");
        result.Provider.Should().Be(CargoProvider.YurticiKargo);
        result.Status.Should().Be(CargoStatus.InTransit);
        result.Events.Should().HaveCount(1);
        result.Events[0].Location.Should().Be("Istanbul");
    }

    [Fact]
    public async Task Handle_ProviderNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var query = new GetShipmentStatusQuery(Guid.NewGuid(), "TRK-99999", CargoProvider.MngKargo);

        _cargoFactory.Setup(f => f.Resolve(CargoProvider.MngKargo)).Returns((ICargoAdapter?)null);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*MngKargo*");
    }

    [Fact]
    public async Task Handle_NoEvents_ReturnsEmptyEventsList()
    {
        // Arrange
        var query = new GetShipmentStatusQuery(Guid.NewGuid(), "TRK-00001", CargoProvider.ArasKargo);

        var trackingResult = new TrackingResult
        {
            TrackingNumber = "TRK-00001",
            Status = CargoStatus.Created,
            Events = new List<TrackingEvent>()
        };

        _cargoFactory.Setup(f => f.Resolve(CargoProvider.ArasKargo)).Returns(_cargoAdapter.Object);
        _cargoAdapter
            .Setup(a => a.TrackShipmentAsync("TRK-00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(trackingResult);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be(CargoStatus.Created);
        result.Events.Should().BeEmpty();
        result.EstimatedDelivery.Should().BeNull();
    }
}
