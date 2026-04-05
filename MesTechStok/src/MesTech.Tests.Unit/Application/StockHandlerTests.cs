using FluentAssertions;
using MesTech.Application.Commands.AddStockLot;
using MesTech.Application.Commands.AdjustStock;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class StockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // ── AddStockLot ──

    [Fact]
    public async Task AddStockLot_ValidCommand_CreatesLotAndMovement()
    {
        var product = new Product { Name = "Test", SKU = "TST-001", Stock = 10 };
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var cmd = new AddStockLotCommand(
            product.Id, "LOT-001", 50, 25.50m, Guid.NewGuid(),
            DateTime.UtcNow.AddYears(1), Guid.NewGuid());
        var handler = new AddStockLotHandler(_productRepo.Object, _movementRepo.Object, _uow.Object, CreateLockService().Object, NullLogger<AddStockLotHandler>.Instance);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddStockLot_ProductNotFound_ReturnsError()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        var cmd = new AddStockLotCommand(Guid.NewGuid(), "LOT", 10, 10m);
        var handler = new AddStockLotHandler(_productRepo.Object, _movementRepo.Object, _uow.Object, CreateLockService().Object, NullLogger<AddStockLotHandler>.Instance);

        var result = await handler.Handle(cmd, CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
    }

    // ── AdjustStock ──

    private static Mock<IDistributedLockService> CreateLockService()
    {
        var lockService = new Mock<IDistributedLockService>();
        lockService.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        return lockService;
    }

    [Fact]
    public async Task AdjustStock_ValidCommand_AdjustsAndRecords()
    {
        var product = new Product { Name = "Test", SKU = "ADJ-001", Stock = 100 };
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var cmd = new AdjustStockCommand(product.Id, -10, "Damaged", "admin");
        var handler = new AdjustStockHandler(_productRepo.Object, _movementRepo.Object, _uow.Object, CreateLockService().Object, Mock.Of<ILogger<AdjustStockHandler>>(), Mock.Of<ITenantProvider>());
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdjustStock_ProductNotFound_Throws()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        var cmd = new AdjustStockCommand(Guid.NewGuid(), 5, "Test", "admin");
        var handler = new AdjustStockHandler(_productRepo.Object, _movementRepo.Object, _uow.Object, CreateLockService().Object, Mock.Of<ILogger<AdjustStockHandler>>(), Mock.Of<ITenantProvider>());

        var result = await handler.Handle(cmd, CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
    }
}
