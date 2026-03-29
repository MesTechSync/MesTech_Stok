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

    private BulkUpdatePriceHandler CreatePriceHandler() =>
        new(_productRepo.Object, _unitOfWork.Object);

    private BulkUpdateStockHandler CreateStockHandler() =>
        new(_productRepo.Object, _unitOfWork.Object, Mock.Of<IDistributedLockService>(), Mock.Of<ILogger<BulkUpdateStockHandler>>());

    // ── BulkUpdatePrice Tests ──

    [Fact]
    public async Task BulkUpdatePrice_TwoValidItems_ShouldReturnSuccessCount2()
    {
        var p1 = FakeData.CreateProduct(sku: "SKU-P001", salePrice: 100m);
        var p2 = FakeData.CreateProduct(sku: "SKU-P002", salePrice: 200m);
        _productRepo.Setup(r => r.GetBySKUAsync("SKU-P001")).ReturnsAsync(p1);
        _productRepo.Setup(r => r.GetBySKUAsync("SKU-P002")).ReturnsAsync(p2);

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
        _productRepo.Setup(r => r.GetBySKUAsync("SKU-P003")).ReturnsAsync(p1);
        _productRepo.Setup(r => r.GetBySKUAsync("UNKNOWN-SKU")).ReturnsAsync((Product?)null);

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
        var command = new BulkUpdatePriceCommand(
        [
            new BulkUpdatePriceItem("SKU-P004", 0m)
        ]);

        var result = await CreatePriceHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Failures[0].Sku.Should().Be("SKU-P004");
        result.Failures[0].Reason.Should().Contain("greater than 0");
        _productRepo.Verify(r => r.GetBySKUAsync(It.IsAny<string>()), Times.Never,
            "Repository should not be called when price is invalid");
    }

    [Fact]
    public async Task BulkUpdatePrice_NegativePrice_ShouldFailWithValidationReason()
    {
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
        var command = new BulkUpdatePriceCommand([]);

        var result = await CreatePriceHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        result.Failures.Should().BeEmpty();
        _productRepo.Verify(r => r.GetBySKUAsync(It.IsAny<string>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── BulkUpdateStock Tests ──

    [Fact]
    public async Task BulkUpdateStock_TwoValidItems_ShouldReturnSuccessCount2()
    {
        var p1 = FakeData.CreateProduct(sku: "SKU-S001", stock: 10);
        var p2 = FakeData.CreateProduct(sku: "SKU-S002", stock: 20);
        _productRepo.Setup(r => r.GetBySKUAsync("SKU-S001")).ReturnsAsync(p1);
        _productRepo.Setup(r => r.GetBySKUAsync("SKU-S002")).ReturnsAsync(p2);

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
        _productRepo.Setup(r => r.GetBySKUAsync("GHOST-SKU")).ReturnsAsync((Product?)null);

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
        var command = new BulkUpdateStockCommand(
        [
            new BulkUpdateStockItem("SKU-S003", -5)
        ]);

        var result = await CreateStockHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Failures[0].Sku.Should().Be("SKU-S003");
        result.Failures[0].Reason.Should().Contain("negative");
        _productRepo.Verify(r => r.GetBySKUAsync(It.IsAny<string>()), Times.Never,
            "Repository should not be called when stock is invalid");
    }

    [Fact]
    public async Task BulkUpdateStock_EmptyList_ShouldReturnZeroCounts()
    {
        var command = new BulkUpdateStockCommand([]);

        var result = await CreateStockHandler().Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        result.Failures.Should().BeEmpty();
        _productRepo.Verify(r => r.GetBySKUAsync(It.IsAny<string>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkUpdateStock_ZeroStock_ShouldSucceed()
    {
        var p1 = FakeData.CreateProduct(sku: "SKU-S004", stock: 50);
        _productRepo.Setup(r => r.GetBySKUAsync("SKU-S004")).ReturnsAsync(p1);

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
