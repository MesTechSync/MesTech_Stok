using FluentAssertions;
using MesTech.Application.Commands.CancelOrder;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class CancelOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CancelOrderHandler CreateHandler() =>
        new(_orderRepo.Object, _unitOfWork.Object);

    [Fact]
    public void Constructor_NullOrderRepository_ShouldThrow()
    {
        var act = () => new CancelOrderHandler(null!, _unitOfWork.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("orderRepository");
    }

    [Fact]
    public void Constructor_NullUnitOfWork_ShouldThrow()
    {
        var act = () => new CancelOrderHandler(_orderRepo.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("unitOfWork");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_OrderFound_ShouldCancelAndReturnSuccess()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Pending);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = CreateHandler();
        var command = new CancelOrderCommand(order.Id, "Customer request");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        order.Status.Should().Be(OrderStatus.Cancelled);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var nonExistentId = Guid.NewGuid();
        _orderRepo.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = CreateHandler();
        var command = new CancelOrderCommand(nonExistentId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(nonExistentId.ToString());
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrderInShippedStatus_ShouldThrowBusinessRuleException()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Shipped);
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = CreateHandler();
        var command = new CancelOrderCommand(order.Id, "Too late");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }
}
