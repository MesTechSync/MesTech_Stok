using FluentAssertions;
using MesTech.Application.DTOs.Shipping;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using MesTech.Application.Features.Shipping.Commands.BatchShipOrders;
using MesTech.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Shipping;

[Trait("Category", "Unit")]
[Trait("Domain", "Shipments")]
public class BatchShipOrdersHandlerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILogger<BatchShipOrdersHandler>> _logger = new();

    private BatchShipOrdersHandler CreateSut() => new(_mediator.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidOrders_ReturnsAggregatedResult()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var command = new BatchShipOrdersCommand(tenantId, orderIds);

        _mediator
            .Setup(m => m.Send(It.IsAny<AutoShipOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AutoShipResult.Succeeded("TRK-001", CargoProvider.YurticiKargo, Guid.NewGuid(), "Best price"));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalOrders.Should().Be(2);
        result.Successful.Should().Be(2);
        result.Failed.Should().Be(0);
        result.Results.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_SomeOrdersFail_ReturnsPartialResult()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var command = new BatchShipOrdersCommand(tenantId, orderIds);

        var callCount = 0;
        _mediator
            .Setup(m => m.Send(It.IsAny<AutoShipOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 2
                    ? AutoShipResult.Failed("Order not found")
                    : AutoShipResult.Succeeded("TRK-" + callCount, CargoProvider.ArasKargo, Guid.NewGuid(), "OK");
            });

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.TotalOrders.Should().Be(3);
        result.Successful.Should().Be(2);
        result.Failed.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ExceptionInAutoShip_CatchesAndContinues()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var command = new BatchShipOrdersCommand(tenantId, orderIds);

        var callCount = 0;
        _mediator
            .Setup(m => m.Send(It.IsAny<AutoShipOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1) throw new InvalidOperationException("DB error");
                return AutoShipResult.Succeeded("TRK-002", CargoProvider.YurticiKargo, Guid.NewGuid(), "OK");
            });

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.TotalOrders.Should().Be(2);
        result.Successful.Should().Be(1);
        result.Failed.Should().Be(1);
        result.Results.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("DB error"));
    }
}
