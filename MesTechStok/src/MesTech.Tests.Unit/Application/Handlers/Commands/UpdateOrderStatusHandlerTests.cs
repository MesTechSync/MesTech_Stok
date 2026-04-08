using FluentAssertions;
using MesTech.Application.Commands.UpdateOrderStatus;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: UpdateOrderStatusHandler testi — sipariş durum geçişleri.
/// P1 iş-kritik: yanlış durum geçişi = sipariş akışı bozulur.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateOrderStatusHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateOrderStatusHandler CreateSut() => new(_orderRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var cmd = new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatus.Confirmed);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ConfirmOrder_ShouldSucceed()
    {
        var order = FakeData.CreateOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var cmd = new UpdateOrderStatusCommand(order.Id, OrderStatus.Confirmed);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShipWithoutTrackingNumber_ShouldFail()
    {
        var order = FakeData.CreateOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var cmd = new UpdateOrderStatusCommand(order.Id, OrderStatus.Shipped);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("TrackingNumber");
    }

    [Fact]
    public async Task Handle_ShipWithTrackingNumber_ShouldSucceed()
    {
        var order = FakeData.CreateOrder();
        order.Place(); // must be confirmed before shipping
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var cmd = new UpdateOrderStatusCommand(order.Id, OrderStatus.Shipped, "TR123456789", CargoProvider.YurticiKargo);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CancelOrder_ShouldSucceed()
    {
        var order = FakeData.CreateOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var cmd = new UpdateOrderStatusCommand(order.Id, OrderStatus.Cancelled);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnsupportedStatus_ShouldReturnError()
    {
        var order = FakeData.CreateOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var cmd = new UpdateOrderStatusCommand(order.Id, (OrderStatus)999);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unsupported");
    }
}
