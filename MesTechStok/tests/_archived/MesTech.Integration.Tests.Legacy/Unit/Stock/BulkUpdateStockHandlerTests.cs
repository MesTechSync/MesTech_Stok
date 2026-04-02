using FluentAssertions;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// BulkUpdateStockHandler: toplu stok güncelleme.
/// Sorted lock ile deadlock prevention, batch SKU lookup ile N+1 önleme.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "StockMovement")]
public class BulkUpdateStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDistributedLockService> _lockService = new();
    private readonly Mock<ILogger<BulkUpdateStockHandler>> _logger = new();
    private readonly Mock<IAsyncDisposable> _lockHandle = new();

    public BulkUpdateStockHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _lockHandle.Setup(h => h.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    private BulkUpdateStockHandler CreateHandler() =>
        new(_productRepo.Object, _uow.Object, _lockService.Object, _logger.Object);

    private void SetupLockAlwaysSuccess()
    {
        _lockService.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandle.Object);
    }

    private Product MakeProduct(string sku, int stock)
    {
        var p = new Product { Name = $"P-{sku}", SKU = sku, PurchasePrice = 10m, SalePrice = 20m, CategoryId = Guid.NewGuid() };
        if (stock > 0) p.AdjustStock(stock, StockMovementType.StockIn);
        return p;
    }

    [Fact]
    public async Task Handle_AllSkusFound_UpdatesAllAndReturnsSuccess()
    {
        SetupLockAlwaysSuccess();
        var p1 = MakeProduct("SKU-A", 10);
        var p2 = MakeProduct("SKU-B", 20);
        var products = new List<Product> { p1, p2 };

        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);
        _productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        var cmd = new BulkUpdateStockCommand(new List<BulkUpdateStockItem>
        {
            new("SKU-A", 50),
            new("SKU-B", 30)
        });

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SkuNotFound_ReportsFailureForMissingSku()
    {
        SetupLockAlwaysSuccess();
        var p1 = MakeProduct("SKU-A", 10);
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { p1 });
        _productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

        var cmd = new BulkUpdateStockCommand(new List<BulkUpdateStockItem>
        {
            new("SKU-A", 50),
            new("MISSING-SKU", 10)
        });

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.Failures.Should().ContainSingle(f => f.Sku == "MISSING-SKU");
    }

    [Fact]
    public async Task Handle_NegativeStock_ReportsFailure()
    {
        SetupLockAlwaysSuccess();
        var p1 = MakeProduct("SKU-A", 10);
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { p1 });

        var cmd = new BulkUpdateStockCommand(new List<BulkUpdateStockItem>
        {
            new("SKU-A", -5)
        });

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.FailedCount.Should().Be(1);
        result.Failures.Should().ContainSingle(f => f.Reason.Contains("negative"));
    }

    [Fact]
    public async Task Handle_LockFails_ReturnsAllFailed()
    {
        _lockService.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var p1 = MakeProduct("SKU-A", 10);
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { p1 });

        var cmd = new BulkUpdateStockCommand(new List<BulkUpdateStockItem>
        {
            new("SKU-A", 50)
        });

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
