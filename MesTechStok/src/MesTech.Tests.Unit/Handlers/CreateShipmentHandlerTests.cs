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

    public CreateShipmentHandlerTests()
    {
        _sut = new CreateShipmentHandler(
            _orderRepoMock.Object, _cargoFactoryMock.Object,
            _uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailed()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var cmd = CreateCommand();
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_TenantMismatch_ReturnsFailed()
    {
        var order = new Order { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() };
        _orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var cmd = CreateCommand();
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("does not belong");
    }

    [Fact]
    public async Task Handle_AlreadyShipped_ReturnsFailed()
    {
        var order = new Order { Id = Guid.NewGuid(), TenantId = _tenantId };
        // Use reflection to set TrackingNumber since it's private set
        typeof(Order).GetProperty("TrackingNumber")!
            .SetValue(order, "TRK123");
        _orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var cmd = CreateCommand();
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already shipped");
    }

    [Fact]
    public async Task Handle_NoAdapter_ReturnsFailed()
    {
        var order = new Order { Id = Guid.NewGuid(), TenantId = _tenantId };
        _orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _cargoFactoryMock.Setup(f => f.Resolve(It.IsAny<CargoProvider>()))
            .Returns((ICargoAdapter?)null);

        var cmd = CreateCommand();
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No cargo adapter");
    }

    private CreateShipmentCommand CreateCommand() => new(
        TenantId: _tenantId,
        OrderId: Guid.NewGuid(),
        CargoProvider: CargoProvider.YurticiKargo,
        RecipientName: "Test Müşteri",
        RecipientAddress: "Test Adres, İstanbul",
        RecipientPhone: "05551234567",
        Weight: 2.5m,
        Notes: null);
}
