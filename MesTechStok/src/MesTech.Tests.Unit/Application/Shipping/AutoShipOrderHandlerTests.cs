using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.DTOs.Shipping;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;
using DomainAutoShipService = MesTech.Domain.Services.IAutoShipmentService;
using DomainShipmentRecommendation = MesTech.Domain.Services.ShipmentRecommendation;
using DomainShipmentRequest = MesTech.Domain.Services.ShipmentRequest;

namespace MesTech.Tests.Unit.Application.Shipping;

[Trait("Category", "Unit")]
[Trait("Domain", "Shipments")]
public class AutoShipOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<DomainAutoShipService> _autoShipService = new();
    private readonly Mock<ICargoProviderFactory> _cargoFactory = new();
    private readonly Mock<ICargoAdapter> _cargoAdapter = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AutoShipOrderHandler CreateSut() =>
        new(_orderRepo.Object, _autoShipService.Object, _cargoFactory.Object, _uow.Object);

    private static Order CreateConfirmedOrder(Guid tenantId, Guid orderId)
    {
        var order = new Order
        {
            OrderNumber = "ORD-001",
            CustomerName = "Test Customer",
            PaymentStatus = "Paid",
            TenantId = tenantId,
            SourcePlatform = PlatformType.Trendyol
        };
        // Set the Id via reflection since BaseEntity may not expose a public setter
        typeof(Order).GetProperty("Id")!.SetValue(order, orderId);
        order.SetFinancials(0m, 0m, 250m);
        order.Place(); // Pending -> Confirmed
        return order;
    }

    [Fact]
    public async Task Handle_ValidOrder_ShipsSuccessfully()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = CreateConfirmedOrder(tenantId, orderId);
        var command = new AutoShipOrderCommand(tenantId, orderId);

        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _autoShipService.Setup(s => s.Recommend(It.IsAny<DomainShipmentRequest>()))
            .Returns(new DomainShipmentRecommendation(CargoProvider.YurticiKargo, "Cheapest option"));
        _cargoFactory.Setup(f => f.Resolve(CargoProvider.YurticiKargo)).Returns(_cargoAdapter.Object);
        _cargoAdapter
            .Setup(a => a.CreateShipmentAsync(It.IsAny<ShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ShipmentResult.Succeeded("TRK-999", Guid.NewGuid().ToString()));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.TrackingNumber.Should().Be("TRK-999");
        result.CargoProvider.Should().Be(CargoProvider.YurticiKargo);
        _orderRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailedResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new AutoShipOrderCommand(Guid.NewGuid(), orderId);

        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_TenantMismatch_ReturnsFailedResult()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = CreateConfirmedOrder(tenantId, orderId);
        var wrongTenantId = Guid.NewGuid();
        var command = new AutoShipOrderCommand(wrongTenantId, orderId);

        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("does not belong to tenant");
    }

    [Fact]
    public async Task Handle_CargoProviderNotResolved_ReturnsFailedResult()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = CreateConfirmedOrder(tenantId, orderId);
        var command = new AutoShipOrderCommand(tenantId, orderId);

        _orderRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _autoShipService.Setup(s => s.Recommend(It.IsAny<DomainShipmentRequest>()))
            .Returns(new DomainShipmentRecommendation(CargoProvider.SuratKargo, "Default"));
        _cargoFactory.Setup(f => f.Resolve(CargoProvider.SuratKargo)).Returns((ICargoAdapter?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No cargo adapter");
    }
}
