using FluentAssertions;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class BulkUpdateStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IDistributedLockService> _lockService;
    private readonly BulkUpdateStockHandler _sut;

    public BulkUpdateStockHandlerTests()
    {
        _productRepo = new Mock<IProductRepository>();
        _uow = new Mock<IUnitOfWork>();
        _lockService = new Mock<IDistributedLockService>();

        // Default: lock always succeeds
        _lockService
            .Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        _sut = new BulkUpdateStockHandler(_productRepo.Object, _uow.Object, _lockService.Object, NullLogger<BulkUpdateStockHandler>.Instance);
    }

    [Fact]
    public async Task Handle_AllValid_ReturnsFullSuccess()
    {
        // Arrange
        var product = new Product { SKU = "SKU-001" };
        product.SyncStock(10);
        var products = new List<Product> { product };
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var command = new BulkUpdateStockCommand(
            new[] { new BulkUpdateStockItem("SKU-001", 20) });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NegativeStock_ReturnsFailure()
    {
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var command = new BulkUpdateStockCommand(
            new[] { new BulkUpdateStockItem("SKU-BAD", -5) });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Failures[0].Reason.Should().Contain("negative");
    }

    [Fact]
    public async Task Handle_SkuNotFound_ReturnsFailure()
    {
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var command = new BulkUpdateStockCommand(
            new[] { new BulkUpdateStockItem("MISSING", 10) });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.FailedCount.Should().Be(1);
        result.Failures[0].Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_MixedItems_ReportsPartialSuccess()
    {
        var product = new Product { SKU = "OK-SKU" };
        product.SyncStock(5);
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var command = new BulkUpdateStockCommand(new[]
        {
            new BulkUpdateStockItem("OK-SKU", 20),
            new BulkUpdateStockItem("BAD-SKU", 10),
            new BulkUpdateStockItem("NEG-SKU", -1)
        });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NoSuccess_DoesNotCallSave()
    {
        _productRepo.Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var command = new BulkUpdateStockCommand(
            new[] { new BulkUpdateStockItem("X", -1) });

        await _sut.Handle(command, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }
}
