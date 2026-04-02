using FluentAssertions;
using MesTech.Application.Commands.CreateProduct;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// CreateProductHandler: ürün oluşturma + SKU uniqueness testi.
/// ProductCreatedEvent → platform adapter sync tetikler.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "ProductCatalog")]
public class CreateProductHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public CreateProductHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _productRepo.Setup(r => r.AddAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
    }

    private CreateProductHandler CreateHandler() =>
        new(_productRepo.Object, _uow.Object);

    private static CreateProductCommand ValidCommand(string sku = "NEW-SKU-001") => new(
        Name: "Yeni Test Ürün",
        SKU: sku,
        Barcode: "8680009999999",
        PurchasePrice: 50m,
        SalePrice: 100m,
        CategoryId: Guid.NewGuid(),
        MinimumStock: 5,
        MaximumStock: 1000,
        TaxRate: 0.18m);

    [Fact]
    public async Task Handle_ValidProduct_ReturnsSuccess_And_PersistsProduct()
    {
        // Arrange — SKU benzersiz
        _productRepo.Setup(r => r.GetBySKUAsync("NEW-SKU-001")).ReturnsAsync((Product?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ProductId.Should().NotBeEmpty();
        _productRepo.Verify(r => r.AddAsync(It.Is<Product>(p =>
            p.Name == "Yeni Test Ürün" &&
            p.SKU == "NEW-SKU-001" &&
            p.Stock == 0)), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSKU_ReturnsFailure()
    {
        // Arrange — SKU zaten var
        var existingProduct = new Product { Name = "Mevcut", SKU = "DUP-SKU", PurchasePrice = 50m, SalePrice = 100m, CategoryId = Guid.NewGuid() };
        _productRepo.Setup(r => r.GetBySKUAsync("DUP-SKU")).ReturnsAsync(existingProduct);

        var handler = CreateHandler();
        var cmd = ValidCommand(sku: "DUP-SKU");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DUP-SKU");
        _productRepo.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductCreated_StartsWithZeroStock()
    {
        // Arrange
        _productRepo.Setup(r => r.GetBySKUAsync(It.IsAny<string>())).ReturnsAsync((Product?)null);

        Product? capturedProduct = null;
        _productRepo.Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(p => capturedProduct = p)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.Handle(ValidCommand(), CancellationToken.None);

        // Assert — ürün 0 stokla oluşturulmalı, AddStock ile eklenmeli
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Stock.Should().Be(0);
        capturedProduct.IsOutOfStock().Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ProductCreated_HasCorrectPricing()
    {
        // Arrange
        _productRepo.Setup(r => r.GetBySKUAsync(It.IsAny<string>())).ReturnsAsync((Product?)null);

        Product? capturedProduct = null;
        _productRepo.Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(p => capturedProduct = p)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.Handle(ValidCommand(), CancellationToken.None);

        // Assert — fiyatlar doğru set edilmeli
        capturedProduct.Should().NotBeNull();
        capturedProduct!.PurchasePrice.Should().Be(50m);
        capturedProduct.SalePrice.Should().Be(100m);
        capturedProduct.TaxRate.Should().Be(0.18m);
    }
}
