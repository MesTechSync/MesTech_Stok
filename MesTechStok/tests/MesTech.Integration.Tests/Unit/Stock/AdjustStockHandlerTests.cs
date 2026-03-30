using FluentAssertions;
using MesTech.Application.Commands.AdjustStock;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// AdjustStockHandler: distributed lock + stok düzeltme.
/// Lock alınamazsa graceful failure döner.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "StockMovement")]
public class AdjustStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDistributedLockService> _lockService = new();
    private readonly Mock<ILogger<AdjustStockHandler>> _logger = new();
    private readonly Mock<IAsyncDisposable> _lockHandle = new();

    public AdjustStockHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _movementRepo.Setup(r => r.AddAsync(It.IsAny<StockMovement>())).Returns(Task.CompletedTask);
        _lockHandle.Setup(h => h.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    private AdjustStockHandler CreateHandler() =>
        new(_productRepo.Object, _movementRepo.Object, _uow.Object, _lockService.Object, _logger.Object);

    private Product CreateProduct(int initialStock)
    {
        var product = new Product { Name = "Test", SKU = "ADJ-001", PurchasePrice = 10m, SalePrice = 20m, CategoryId = Guid.NewGuid() };
        if (initialStock > 0)
            product.AdjustStock(initialStock, StockMovementType.StockIn);
        return product;
    }

    private void SetupLockSuccess(Guid productId)
    {
        _lockService.Setup(l => l.AcquireLockAsync(
                $"stock:product:{productId}",
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandle.Object);
    }

    [Fact]
    public async Task Handle_ValidAdjustment_ChangesStockAndRecordsMovement()
    {
        var product = CreateProduct(100);
        SetupLockSuccess(product.Id);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cmd = new AdjustStockCommand(product.Id, -20, "Sayım farkı", "admin");
        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(80);
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LockUnavailable_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        _lockService.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var cmd = new AdjustStockCommand(productId, 10, "test");
        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("kilit");
        _productRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        var missingId = Guid.NewGuid();
        SetupLockSuccess(missingId);
        _productRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Product?)null);

        var cmd = new AdjustStockCommand(missingId, 5, "test");
        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
