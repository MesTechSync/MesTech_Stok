using FluentAssertions;
using MesTech.Application.Commands.BulkUpdatePrice;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class BulkUpdateHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDistributedLockService> _lockService = new();

    public BulkUpdateHandlerTests()
    {
        // Default: lock service returns a disposable handle (lock acquired)
        _lockService
            .Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
    }

    private BulkUpdatePriceHandler CreatePriceHandler() =>
        new(_productRepo.Object, _unitOfWork.Object);

    private BulkUpdateStockHandler CreateStockHandler() =>
        new(_productRepo.Object, _unitOfWork.Object, _lockService.Object, Mock.Of<ILogger<BulkUpdateStockHandler>>());

    // ── BulkUpdatePrice Tests ──

    [Fact]
    public async Task BulkUpdatePrice_TwoValidItems_ShouldReturnSuccessCount2()
    {
        var p1 = FakeData.CreateProduct(sku: "SKU-P001", salePrice: 100m);
        var p2 = FakeData.CreateProduct(sku: "SKU-P002", salePrice: 200m);
        _productRepo
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { p1, p2 });

        var command = new BulkUpdatePriceCommand(
        [
            new BulkUpdatePriceItem("SKU-P001", 150m),
            new BulkUpdatePriceItem("SKU-P002", 250m)
        ]);

        var result = await CreatePriceHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.Failures.Should().BeEmpty();
        p1.SalePrice.Should().Be(150m);
        p2.SalePrice.Should().Be(250m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdatePrice_OneValidOneUnknownSku_ShouldReturn1Success1Failure()
    {
        var p1 = FakeData.CreateProduct(sku: "SKU-P003", salePrice: 100m);
        _productRepo
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { p1 });

        var command = new BulkUpdatePriceCommand(
        [
            new BulkUpdatePriceItem("SKU-P003", 120m),
            new BulkUpdatePriceItem("UNKNOWN-SKU", 99m)
        ]);

        var result = await CreatePriceHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.Failures.Should().HaveCount(1);
        result.Failures[0].Sku.Should().Be("UNKNOWN-SKU");
        result.Failures[0].Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task BulkUpdatePrice_PriceZeroOrNegative_ShouldFailWithValidationReason()
    {
        _productRepo
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var command = new BulkUpdatePriceCommand(
        [
            new BulkUpdatePriceItem("SKU-P004", 0m)
        ]);

        var result = await CreatePriceHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Failures[0].Sku.Should().Be("SKU-P004");
        result.Failures[0].Reason.Should().Contain("greater than 0");
    }

    [Fact]
    public async Task BulkUpdatePrice_NegativePrice_ShouldFailWithValidationReason()
    {
        _productRepo
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var command = new BulkUpdatePriceCommand(
        [
            new BulkUpdatePriceItem("SKU-P005", -10m)
        ]);

        var result = await CreatePriceHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Failures[0].Reason.Should().Contain("greater than 0");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkUpdatePrice_EmptyList_ShouldReturnZeroCounts()
    {
        _productRepo
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var command = new BulkUpdatePriceCommand([]);

        var result = await CreatePriceHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        result.Failures.Should().BeEmpty();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── BulkUpdateStock Tests ──

    [Fact]
    public async Task BulkUpdateStock_TwoValidItems_ShouldReturnSuccessCount2()
    {
        var p1 = FakeData.CreateProduct(sku: "SKU-S001", stock: 10);
        var p2 = FakeData.CreateProduct(sku: "SKU-S002", stock: 20);
        _productRepo
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { p1, p2 });

        var command = new BulkUpdateStockCommand(
        [
            new BulkUpdateStockItem("SKU-S001", 100),
            new BulkUpdateStockItem("SKU-S002", 200)
        ]);

        var result = await CreateStockHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.Failures.Should().BeEmpty();
        p1.Stock.Should().Be(100);
        p2.Stock.Should().Be(200);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateStock_UnknownSku_ShouldFailWithSkuNotFound()
    {
        _productRepo
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var command = new BulkUpdateStockCommand(
        [
            new BulkUpdateStockItem("GHOST-SKU", 50)
        ]);

        var result = await CreateStockHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Failures[0].Sku.Should().Be("GHOST-SKU");
        result.Failures[0].Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task BulkUpdateStock_NegativeStock_ShouldFailWithNegativeReason()
    {
        _productRepo
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var command = new BulkUpdateStockCommand(
        [
            new BulkUpdateStockItem("SKU-S003", -5)
        ]);

        var result = await CreateStockHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Failures[0].Sku.Should().Be("SKU-S003");
        result.Failures[0].Reason.Should().Contain("negative");
    }

    [Fact]
    public async Task BulkUpdateStock_EmptyList_ShouldReturnZeroCounts()
    {
        _productRepo
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var command = new BulkUpdateStockCommand([]);

        var result = await CreateStockHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        result.Failures.Should().BeEmpty();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkUpdateStock_ZeroStock_ShouldSucceed()
    {
        var p1 = FakeData.CreateProduct(sku: "SKU-S004", stock: 50);
        _productRepo
            .Setup(r => r.GetBySKUsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { p1 });

        var command = new BulkUpdateStockCommand(
        [
            new BulkUpdateStockItem("SKU-S004", 0)
        ]);

        var result = await CreateStockHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
        p1.Stock.Should().Be(0, "zero stock is valid (out of stock)");
    }
}
