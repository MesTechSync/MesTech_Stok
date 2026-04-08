using FluentAssertions;
using MesTech.Application.Commands.UpdateProductImage;
using MesTech.Application.Queries.GetProductByBarcode;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

// ════════════════════════════════════════════════════════
// Task 9: Product Image CQRS Handler Tests
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
public class GetProductByBarcodeHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    private GetProductByBarcodeHandler CreateHandler() => new(_productRepo.Object);

    [Fact]
    public async Task Handle_ExistingBarcode_ReturnsProductDto()
    {
        // Arrange
        var product = FakeData.CreateProduct(
            sku: "IMG-001", barcode: "8680000999999",
            stock: 50, purchasePrice: 80m, salePrice: 120m,
            minimumStock: 5);
        _productRepo.Setup(r => r.GetByBarcodeAsync("8680000999999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetProductByBarcodeQuery("8680000999999"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SKU.Should().Be("IMG-001");
        result.Barcode.Should().Be("8680000999999");
        result.StockStatus.Should().Be("Normal");
        result.ProfitMargin.Should().Be(product.ProfitMargin);
        result.TotalValue.Should().Be(product.TotalValue);
        result.NeedsReorder.Should().Be(product.NeedsReorder());
    }

    [Fact]
    public async Task Handle_NonExistentBarcode_ReturnsNull()
    {
        // Arrange
        _productRepo.Setup(r => r.GetByBarcodeAsync("0000000000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetProductByBarcodeQuery("0000000000000"), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}

[Trait("Category", "Unit")]
public class UpdateProductImageHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateProductImageHandler CreateHandler() =>
        new(_productRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_ValidProduct_UpdatesImageAndReturnsSuccess()
    {
        // Arrange
        var product = FakeData.CreateProduct(sku: "UPD-001");
        var productId = product.Id;
        var newImageUrl = "https://cdn.example.com/images/product-001.jpg";

        _productRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateProductImageCommand(productId, newImageUrl), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        product.ImageUrl.Should().Be(newImageUrl);
        _productRepo.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentProduct_ReturnsError()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateProductImageCommand(missingId, "https://cdn.example.com/x.jpg"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        _productRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
