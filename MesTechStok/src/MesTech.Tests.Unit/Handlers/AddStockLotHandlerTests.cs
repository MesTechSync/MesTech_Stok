using FluentAssertions;
using MesTech.Application.Commands.AddStockLot;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class AddStockLotHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IStockMovementRepository> _movementRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDistributedLockService> _lockService = new();
    private readonly AddStockLotHandler _sut;

    public AddStockLotHandlerTests()
    {
        _lockService.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        _sut = new AddStockLotHandler(_productRepoMock.Object, _movementRepoMock.Object, _uowMock.Object, _lockService.Object, NullLogger<AddStockLotHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesLotAndReturnsSuccess()
    {
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "Test", Description = "DESC", Stock = 10, TenantId = Guid.NewGuid(), SKU = "TST-001" };
        _productRepoMock.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var cmd = new AddStockLotCommand(productId, "LOT-001", 5, 10.5m);
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ZeroQuantity_ReturnsFail()
    {
        var cmd = new AddStockLotCommand(Guid.NewGuid(), "LOT-001", 0, 10m);
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("pozitif");
    }

    [Fact]
    public async Task Handle_NegativeQuantity_ReturnsFail()
    {
        var cmd = new AddStockLotCommand(Guid.NewGuid(), "LOT-001", -5, 10m);
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_EmptyLotNumber_ReturnsFail()
    {
        var cmd = new AddStockLotCommand(Guid.NewGuid(), "", 5, 10m);
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Lot numarası");
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFail()
    {
        var productId = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var cmd = new AddStockLotCommand(productId, "LOT-001", 5, 10m);
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
