using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ReturnApprovedRefundHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ReturnApprovedRefundHandler>> _logger = new();

    private ReturnApprovedRefundHandler CreateSut() =>
        new(_orderRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ZeroRefundAmount_ShouldSkip()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, CancellationToken.None);

        _orderRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NegativeRefundAmount_ShouldSkip()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -50m, CancellationToken.None);

        _orderRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_OrderNotFound_ShouldReturnWithoutSaving()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 250m, CancellationToken.None);

        _orderRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValidRefund_ShouldUpdateOrderNotesAndSave()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Shipped);
        order.Notes = null;
        var orderId = order.Id;

        _orderRepo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var sut = CreateSut();
        var returnId = Guid.NewGuid();
        await sut.HandleAsync(returnId, orderId, Guid.NewGuid(), 150m, CancellationToken.None);

        order.Notes.Should().Contain("REFUND");
        order.Notes.Should().Contain("150");
        order.Notes.Should().Contain(returnId.ToString());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExistingNotes_ShouldAppendRefundInfo()
    {
        var order = FakeData.CreateOrder(status: OrderStatus.Shipped);
        order.Notes = "Previous note";
        var orderId = order.Id;

        _orderRepo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), orderId, Guid.NewGuid(), 100m, CancellationToken.None);

        order.Notes.Should().StartWith("Previous note");
        order.Notes.Should().Contain("REFUND");
    }
}
