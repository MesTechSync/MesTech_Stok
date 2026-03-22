using FluentAssertions;
using MesTech.Application.Features.Product.Commands.BulkUpdateProducts;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Products;

[Trait("Category", "Unit")]
[Trait("Domain", "Products")]
public class BulkUpdateProductsHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<BulkUpdateProductsHandler>> _logger = new();

    private BulkUpdateProductsHandler CreateSut() =>
        new(_productRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidRequest_PriceIncreasePercent_ReturnsUpdatedCount()
    {
        // Arrange
        var product = FakeData.CreateProduct(sku: "BULK-001", salePrice: 100m);
        var productId = product.Id;
        _productRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        var command = new BulkUpdateProductsCommand(
            new List<Guid> { productId },
            BulkUpdateAction.PriceIncreasePercent,
            10m);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        product.SalePrice.Should().Be(110m);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullOrEmptyProductIds_ReturnsZero()
    {
        // Arrange
        var command = new BulkUpdateProductsCommand(
            new List<Guid>(),
            BulkUpdateAction.StatusActivate);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _productRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductNotFound_SkipsAndReturnsZero()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Product?)null);

        var command = new BulkUpdateProductsCommand(
            new List<Guid> { missingId },
            BulkUpdateAction.StatusActivate);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StatusDeactivate_SetsIsActiveFalse()
    {
        // Arrange
        var product = FakeData.CreateProduct(sku: "BULK-002");
        product.IsActive.Should().BeTrue();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var command = new BulkUpdateProductsCommand(
            new List<Guid> { product.Id },
            BulkUpdateAction.StatusDeactivate);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PriceSetFixed_SetsExactPrice()
    {
        // Arrange
        var product = FakeData.CreateProduct(sku: "BULK-003", salePrice: 200m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var command = new BulkUpdateProductsCommand(
            new List<Guid> { product.Id },
            BulkUpdateAction.PriceSetFixed,
            350m);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        product.SalePrice.Should().Be(350m);
    }

    [Fact]
    public async Task Handle_MultipleProducts_MixedFoundAndMissing_ReturnsCorrectCount()
    {
        // Arrange
        var p1 = FakeData.CreateProduct(sku: "BULK-MIX1", salePrice: 100m);
        var missingId = Guid.NewGuid();
        var p3 = FakeData.CreateProduct(sku: "BULK-MIX3", salePrice: 300m);

        _productRepo.Setup(r => r.GetByIdAsync(p1.Id)).ReturnsAsync(p1);
        _productRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Product?)null);
        _productRepo.Setup(r => r.GetByIdAsync(p3.Id)).ReturnsAsync(p3);

        var command = new BulkUpdateProductsCommand(
            new List<Guid> { p1.Id, missingId, p3.Id },
            BulkUpdateAction.StatusActivate);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(2);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
