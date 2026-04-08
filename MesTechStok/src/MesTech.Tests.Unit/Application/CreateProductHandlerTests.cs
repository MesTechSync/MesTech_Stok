using FluentAssertions;
using MesTech.Application.Commands.CreateProduct;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class CreateProductHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateProduct()
    {
        _productRepo.Setup(r => r.GetBySKUAsync("NEW-SKU", It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = new CreateProductHandler(_productRepo.Object, _unitOfWork.Object, Mock.Of<ITenantProvider>());
        var command = new CreateProductCommand("Test Product", "NEW-SKU", "1234567890123", 100, 150, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _productRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSKU_ShouldReturnError()
    {
        var existing = new Product { SKU = "EXISTING-SKU" };
        _productRepo.Setup(r => r.GetBySKUAsync("EXISTING-SKU", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var handler = new CreateProductHandler(_productRepo.Object, _unitOfWork.Object, Mock.Of<ITenantProvider>());
        var command = new CreateProductCommand("Test", "EXISTING-SKU", null, 100, 150, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("EXISTING-SKU");
    }

    [Fact]
    public async Task Handle_ShouldSetActiveTrue()
    {
        _productRepo.Setup(r => r.GetBySKUAsync("ACT-001", It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        Product? capturedProduct = null;
        _productRepo.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p);

        var handler = new CreateProductHandler(_productRepo.Object, _unitOfWork.Object, Mock.Of<ITenantProvider>());
        var command = new CreateProductCommand("Active Product", "ACT-001", null, 50, 100, Guid.NewGuid());

        await handler.Handle(command, CancellationToken.None);

        capturedProduct.Should().NotBeNull();
        capturedProduct!.IsActive.Should().BeTrue();
    }
}
