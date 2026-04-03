using FluentAssertions;
using MesTech.Application.Features.Stock.Commands.CreateStockLot;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class CreateStockLotHandlerTests
{
    private readonly Mock<IStockLotRepository> _lotRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateStockLotHandler>> _logger = new();

    private CreateStockLotHandler CreateHandler() =>
        new(_lotRepo.Object, _unitOfWork.Object, _logger.Object);

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateLotAndReturnSuccess()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new CreateStockLotCommand(
            tenantId, productId, "LOT-2026-001", 100, 25.50m,
            Notes: "Test lot");

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.LotId.Should().NotBeEmpty();
        result.ErrorMessage.Should().BeNull();
        _lotRepo.Verify(r => r.AddAsync(It.IsAny<StockLot>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidLotNumber_ShouldReturnFailure()
    {
        var command = new CreateStockLotCommand(
            Guid.NewGuid(), Guid.NewGuid(), "", 100, 25.50m);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ZeroQuantity_ShouldReturnFailure()
    {
        var command = new CreateStockLotCommand(
            Guid.NewGuid(), Guid.NewGuid(), "LOT-001", 0, 10m);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
