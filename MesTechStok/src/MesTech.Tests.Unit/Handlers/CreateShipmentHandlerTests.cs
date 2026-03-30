using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Features.Shipping.Commands.CreateShipment;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateShipmentHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<ICargoProviderFactory> _cargoFactoryMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<CreateShipmentHandler>> _loggerMock = new();
    private readonly CreateShipmentHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _orderId = Guid.NewGuid();

    public CreateShipmentHandlerTests()
    {
        _sut = new CreateShipmentHandler(
            _orderRepoMock.Object,
            _cargoFactoryMock.Object,
            _uowMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailedResult()
    {
        // Arrange
        _orderRepoMock.Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var command = CreateCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(_orderId.ToString());
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_OrderBelongsToDifferentTenant_ReturnsFailedResult()
    {
        // Arrange
        var otherTenantId = Guid.NewGuid();
        var order = CreateOrder(otherTenantId, OrderStatus.Confirmed);

        _orderRepoMock.Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = CreateCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("does not belong to tenant");
    }

    [Fact]
    public async Task Handle_OrderAlreadyShipped_ReturnsFailedResult()
    {
        // Arrange
        var order = CreateOrder(_tenantId, OrderStatus.Confirmed);
        order.MarkAsShipped("TRK-123", CargoProvider.YurticiKargo);

        _orderRepoMock.Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = CreateCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already shipped");
    }

    [Fact]
    public async Task Handle_NoAdapterForProvider_ReturnsFailedResult()
    {
        // Arrange
        var order = CreateOrder(_tenantId, OrderStatus.Confirmed);

        _orderRepoMock.Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _cargoFactoryMock.Setup(f => f.Resolve(It.IsAny<CargoProvider>()))
            .Returns((ICargoAdapter?)null);

        var command = CreateCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No cargo adapter");
    }

    [Fact]
    public async Task Handle_AdapterReturnsFailure_ReturnsFailedResult()
    {
        // Arrange
        var order = CreateOrder(_tenantId, OrderStatus.Confirmed);
        var adapterMock = new Mock<ICargoAdapter>();
        adapterMock.Setup(a => a.CreateShipmentAsync(It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ShipmentResult.Failed("Cargo API error"));

        _orderRepoMock.Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _cargoFactoryMock.Setup(f => f.Resolve(CargoProvider.ArasKargo))
            .Returns(adapterMock.Object);

        var command = CreateCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cargo API error");
    }

    [Fact]
    public async Task Handle_SuccessfulShipment_ReturnsSuccessAndUpdatesOrder()
    {
        // Arrange
        var order = CreateOrder(_tenantId, OrderStatus.Confirmed);
        var adapterMock = new Mock<ICargoAdapter>();
        adapterMock.Setup(a => a.CreateShipmentAsync(It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ShipmentResult.Succeeded("TRK-456", "SHP-789"));

        _orderRepoMock.Setup(r => r.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _cargoFactoryMock.Setup(f => f.Resolve(CargoProvider.ArasKargo))
            .Returns(adapterMock.Object);

        var command = CreateCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TrackingNumber.Should().Be("TRK-456");
        result.CargoBarcode.Should().Be("SHP-789");

        _orderRepoMock.Verify(r => r.UpdateAsync(order), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private CreateShipmentCommand CreateCommand() =>
        new(
            TenantId: _tenantId,
            OrderId: _orderId,
            CargoProvider: CargoProvider.ArasKargo,
            RecipientName: "Test Customer",
            RecipientAddress: "Test Street 123",
            RecipientPhone: "+905551234567",
            Weight: 2.5m,
            Notes: "Fragile");

    private Order CreateOrder(Guid tenantId, OrderStatus status)
    {
        var order = new Order
        {
            Id = _orderId,
            TenantId = tenantId,
            OrderNumber = "ORD-001",
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow
        };

        if (status == OrderStatus.Confirmed)
            order.Place();

        return order;
    }
}
