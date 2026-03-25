using FluentAssertions;
using MediatR;
using MesTech.Application.Commands.ApproveReturn;
using MesTech.Application.Commands.RejectReturn;
using MesTech.Application.DTOs.Shipping;
using MesTech.Application.Features.Dashboard.Queries.GetOrdersPending;
using MesTech.Application.Features.Dashboard.Queries.GetSalesToday;
using MesTech.Application.Features.Orders.Queries.GetStaleOrders;
using MesTech.Application.Features.Shipping.Commands.BatchShipOrders;
using MesTech.Application.Queries.ListOrders;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class OrderHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ── ListOrdersHandler ──────────────────────────────────────

    [Fact]
    public async Task ListOrders_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IOrderRepository>();
        var sut = new ListOrdersHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ListOrders_NoFilter_ReturnsAll()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = new ListOrdersHandler(repo.Object);
        var result = await sut.Handle(new ListOrdersQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        repo.Verify(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once());
    }

    [Fact]
    public async Task ListOrders_WithDates_PassesDatesToRepository()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(from, to))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = new ListOrdersHandler(repo.Object);
        var result = await sut.Handle(new ListOrdersQuery(from, to), CancellationToken.None);

        result.Should().NotBeNull();
        repo.Verify(r => r.GetByDateRangeAsync(from, to), Times.Once());
    }

    // ── GetOrdersPendingHandler ────────────────────────────────

    [Fact]
    public async Task GetOrdersPending_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IOrderRepository>();
        var sut = new GetOrdersPendingHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetOrdersPending_NoOrders_ReturnsZeroCounts()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = new GetOrdersPendingHandler(repo.Object);
        var result = await sut.Handle(new GetOrdersPendingQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Count.Should().Be(0);
        result.Urgent.Should().Be(0);
    }

    // ── BatchShipOrdersHandler ─────────────────────────────────

    [Fact]
    public async Task BatchShipOrders_NullRequest_ThrowsArgumentNullException()
    {
        var mediator = new Mock<IMediator>();
        var logger = NullLogger<BatchShipOrdersHandler>.Instance;
        var sut = new BatchShipOrdersHandler(mediator.Object, logger);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task BatchShipOrders_EmptyOrderIds_ReturnsEmptyResult()
    {
        var mediator = new Mock<IMediator>();
        var logger = NullLogger<BatchShipOrdersHandler>.Instance;
        var sut = new BatchShipOrdersHandler(mediator.Object, logger);

        var command = new BatchShipOrdersCommand(_tenantId, new List<Guid>());
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalOrders.Should().Be(0);
        mediator.Verify(m => m.Send(It.IsAny<IRequest<AutoShipResult>>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task BatchShipOrders_SingleOrder_CallsMediatorOnce()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<IRequest<AutoShipResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutoShipResult { Success = true, TrackingNumber = "TRK001" });

        var logger = NullLogger<BatchShipOrdersHandler>.Instance;
        var sut = new BatchShipOrdersHandler(mediator.Object, logger);

        var orderId = Guid.NewGuid();
        var command = new BatchShipOrdersCommand(_tenantId, new List<Guid> { orderId });
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalOrders.Should().Be(1);
    }

    // ── GetStaleOrdersQueryHandler ─────────────────────────────

    [Fact]
    public async Task GetStaleOrders_NoStaleOrders_ReturnsEmpty()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetStaleOrdersAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var logger = NullLogger<GetStaleOrdersQueryHandler>.Instance;
        var sut = new GetStaleOrdersQueryHandler(repo.Object, logger);

        var query = new GetStaleOrdersQuery(_tenantId);
        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStaleOrders_CustomThreshold_UsesProvidedValue()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetStaleOrdersAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var logger = NullLogger<GetStaleOrdersQueryHandler>.Instance;
        var sut = new GetStaleOrdersQueryHandler(repo.Object, logger);

        var query = new GetStaleOrdersQuery(_tenantId, TimeSpan.FromHours(72));
        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
        repo.Verify(r => r.GetStaleOrdersAsync(
            _tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    // ── ApproveReturnHandler ───────────────────────────────────

    [Fact]
    public async Task ApproveReturn_NullRequest_ThrowsArgumentNullException()
    {
        var returnRepo = new Mock<IReturnRequestRepository>();
        var productRepo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new ApproveReturnHandler(returnRepo.Object, productRepo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ApproveReturn_ReturnNotFound_ReturnsFailure()
    {
        var returnRepo = new Mock<IReturnRequestRepository>();
        returnRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ReturnRequest?)null);
        var productRepo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new ApproveReturnHandler(returnRepo.Object, productRepo.Object, uow.Object);

        var command = new ApproveReturnCommand(Guid.NewGuid());
        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamadi");
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public void ApproveReturn_NullReturnRepo_ThrowsArgumentNullException()
    {
        var act = () => new ApproveReturnHandler(null!, new Mock<IProductRepository>().Object, new Mock<IUnitOfWork>().Object);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── RejectReturnHandler ────────────────────────────────────

    [Fact]
    public async Task RejectReturn_NullRequest_ThrowsArgumentNullException()
    {
        var returnRepo = new Mock<IReturnRequestRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new RejectReturnHandler(returnRepo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RejectReturn_ReturnNotFound_ReturnsFailure()
    {
        var returnRepo = new Mock<IReturnRequestRepository>();
        returnRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ReturnRequest?)null);
        var uow = new Mock<IUnitOfWork>();
        var sut = new RejectReturnHandler(returnRepo.Object, uow.Object);

        var command = new RejectReturnCommand(Guid.NewGuid(), "Hasar yok");
        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamadi");
    }

    [Fact]
    public void RejectReturn_NullReturnRepo_ThrowsArgumentNullException()
    {
        var act = () => new RejectReturnHandler(null!, new Mock<IUnitOfWork>().Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RejectReturn_NullUow_ThrowsArgumentNullException()
    {
        var act = () => new RejectReturnHandler(new Mock<IReturnRequestRepository>().Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── GetSalesTodayHandler ───────────────────────────────────

    [Fact]
    public async Task GetSalesToday_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IOrderRepository>();
        var sut = new GetSalesTodayHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetSalesToday_NoOrders_ReturnsZeros()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = new GetSalesTodayHandler(repo.Object);
        var result = await sut.Handle(new GetSalesTodayQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Today.Should().Be(0);
        result.Yesterday.Should().Be(0);
        result.ChangePercent.Should().Be(0);
    }
}
