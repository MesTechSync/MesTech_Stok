using FluentAssertions;
using MesTech.Application.Commands.UpdateProduct;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// UpdateProductHandler: ürün güncelleme — conditional field update.
/// Kritik iş kuralları:
///   - Sadece verilen alanlar güncellenmeli (null/HasValue kontrol)
///   - Product.UpdatePrice zarar kontrolü tetikler (Z10)
///   - Product.MarkAsUpdated çağrılmalı
///   - Ürün bulunamazsa hata dönmeli
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "ProductCatalog")]
public class UpdateProductHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public UpdateProductHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _productRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
    }

    private UpdateProductHandler CreateHandler() => new(_productRepo.Object, _uow.Object);

    private Product CreateProduct() => new()
    {
        Name = "Eski Ürün", SKU = "OLD-001", PurchasePrice = 50m, SalePrice = 100m,
        CategoryId = Guid.NewGuid(), MinimumStock = 5, MaximumStock = 1000
    };

    [Fact]
    public async Task Handle_UpdateName_OnlyNameChanges()
    {
        var product = CreateProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cmd = new UpdateProductCommand(product.Id, Name: "Yeni Ürün");
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("Yeni Ürün");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var cmd = new UpdateProductCommand(Guid.NewGuid(), Name: "X");
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _productRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdatePrice_TriggersUpdatePrice()
    {
        var product = CreateProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cmd = new UpdateProductCommand(product.Id, SalePrice: 80m);
        var handler = CreateHandler();

        await handler.Handle(cmd, CancellationToken.None);

        product.SalePrice.Should().Be(80m);
    }

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var handler = CreateHandler();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
