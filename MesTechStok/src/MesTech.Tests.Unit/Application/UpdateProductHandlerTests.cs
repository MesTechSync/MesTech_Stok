using FluentAssertions;
using MesTech.Application.Commands.UpdateProduct;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class UpdateProductHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateProductHandler CreateHandler() =>
        new(_productRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_ValidUpdate_ShouldModifyProduct()
    {
        var product = FakeData.CreateProduct(sku: "UPD-001", salePrice: 100m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new UpdateProductCommand(product.Id, Name: "Updated Name", SalePrice: 200m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("Updated Name");
        product.SalePrice.Should().Be(200m);
        _productRepo.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnError()
    {
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = CreateHandler();
        var command = new UpdateProductCommand(missingId, Name: "Ghost");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
    }

    [Fact]
    public async Task Handle_PartialUpdate_ShouldOnlyChangeSpecifiedFields()
    {
        var product = FakeData.CreateProduct(sku: "PART-001", purchasePrice: 50m, salePrice: 100m);
        var originalName = product.Name;
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new UpdateProductCommand(product.Id, PurchasePrice: 75m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.PurchasePrice.Should().Be(75m);
        product.SalePrice.Should().Be(100m, "SalePrice was not in command");
        product.Name.Should().Be(originalName, "Name was not in command");
    }

    [Fact]
    public async Task Handle_DeactivateProduct_ShouldSetIsActiveFalse()
    {
        var product = FakeData.CreateProduct(sku: "DEACT-001");
        product.IsActive.Should().BeTrue("product starts active");
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new UpdateProductCommand(product.Id, IsActive: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UpdateAllFields_ShouldApplyAll()
    {
        var product = FakeData.CreateProduct(sku: "FULL-001");
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var newCat = Guid.NewGuid();
        var newSup = Guid.NewGuid();
        var newWh = Guid.NewGuid();
        var handler = CreateHandler();
        var command = new UpdateProductCommand(
            product.Id,
            Name: "Full Update",
            Description: "Desc",
            PurchasePrice: 10m,
            SalePrice: 20m,
            ListPrice: 25m,
            TaxRate: 0.08m,
            CategoryId: newCat,
            SupplierId: newSup,
            WarehouseId: newWh,
            MinimumStock: 2,
            MaximumStock: 500,
            Brand: "NewBrand",
            IsActive: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("Full Update");
        product.Description.Should().Be("Desc");
        product.PurchasePrice.Should().Be(10m);
        product.SalePrice.Should().Be(20m);
        product.ListPrice.Should().Be(25m);
        product.TaxRate.Should().Be(0.08m);
        product.CategoryId.Should().Be(newCat);
        product.SupplierId.Should().Be(newSup);
        product.WarehouseId.Should().Be(newWh);
        product.MinimumStock.Should().Be(2);
        product.MaximumStock.Should().Be(500);
        product.Brand.Should().Be("NewBrand");
        product.IsActive.Should().BeFalse();
    }
}
