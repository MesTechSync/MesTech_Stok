using FluentAssertions;
using MesTech.Application.Features.Product.Commands.BulkUpdateProducts;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// BulkUpdateProductsHandler: toplu ürün güncelleme — 10 action tipi.
/// Batch resilience: tek ürün hatası diğerlerini etkilemez.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "BulkOperations")]
public class BulkUpdateProductsHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<BulkUpdateProductsHandler>> _logger = new();

    public BulkUpdateProductsHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
    }

    private BulkUpdateProductsHandler CreateHandler() =>
        new(_productRepo.Object, _uow.Object, _logger.Object);

    private Product CreateProduct(decimal salePrice = 100m, int stock = 50) =>
        new() { Name = "Bulk Ürün", SKU = "BLK-001", SalePrice = salePrice, PurchasePrice = 50m, Stock = stock, CategoryId = Guid.NewGuid() };

    [Fact]
    public async Task Handle_PriceIncreasePercent_IncreasesPrice()
    {
        var product = CreateProduct(salePrice: 100m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cmd = new BulkUpdateProductsCommand(
            new List<Guid> { product.Id },
            BulkUpdateAction.PriceIncreasePercent,
            "20"); // %20 artış

        var handler = CreateHandler();
        var count = await handler.Handle(cmd, CancellationToken.None);

        count.Should().Be(1);
        product.SalePrice.Should().Be(120m); // 100 * 1.20
    }

    [Fact]
    public async Task Handle_PriceDecreasePercent_DecreasesPrice()
    {
        var product = CreateProduct(salePrice: 200m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cmd = new BulkUpdateProductsCommand(
            new List<Guid> { product.Id },
            BulkUpdateAction.PriceDecreasePercent,
            "10"); // %10 indirim

        var handler = CreateHandler();
        var count = await handler.Handle(cmd, CancellationToken.None);

        count.Should().Be(1);
        product.SalePrice.Should().Be(180m); // 200 * 0.90
    }

    [Fact]
    public async Task Handle_StatusDeactivate_DeactivatesProduct()
    {
        var product = CreateProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cmd = new BulkUpdateProductsCommand(
            new List<Guid> { product.Id },
            BulkUpdateAction.StatusDeactivate);

        var handler = CreateHandler();
        await handler.Handle(cmd, CancellationToken.None);

        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_EmptyProductList_ReturnsZero()
    {
        var cmd = new BulkUpdateProductsCommand(
            new List<Guid>(),
            BulkUpdateAction.PriceSetFixed,
            "99.99");

        var handler = CreateHandler();
        var count = await handler.Handle(cmd, CancellationToken.None);

        count.Should().Be(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductNotFound_SkipsAndContinues()
    {
        var existingProduct = CreateProduct();
        var missingId = Guid.NewGuid();

        _productRepo.Setup(r => r.GetByIdAsync(existingProduct.Id)).ReturnsAsync(existingProduct);
        _productRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Product?)null);

        var cmd = new BulkUpdateProductsCommand(
            new List<Guid> { missingId, existingProduct.Id },
            BulkUpdateAction.StatusActivate);

        var handler = CreateHandler();
        var count = await handler.Handle(cmd, CancellationToken.None);

        // Missing product atlanır, existing güncellenir
        count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Handle_MultipleProducts_UpdatesAll()
    {
        var p1 = CreateProduct(salePrice: 100m);
        var p2 = CreateProduct(salePrice: 200m);

        _productRepo.Setup(r => r.GetByIdAsync(p1.Id)).ReturnsAsync(p1);
        _productRepo.Setup(r => r.GetByIdAsync(p2.Id)).ReturnsAsync(p2);

        var cmd = new BulkUpdateProductsCommand(
            new List<Guid> { p1.Id, p2.Id },
            BulkUpdateAction.PriceSetFixed,
            "150");

        var handler = CreateHandler();
        var count = await handler.Handle(cmd, CancellationToken.None);

        count.Should().Be(2);
        p1.SalePrice.Should().Be(150m);
        p2.SalePrice.Should().Be(150m);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
