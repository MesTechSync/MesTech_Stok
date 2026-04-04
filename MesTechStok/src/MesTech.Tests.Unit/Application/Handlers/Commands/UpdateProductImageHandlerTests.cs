using FluentAssertions;
using MesTech.Application.Commands.UpdateProductImage;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: UpdateProductImageHandler testi — ürün görsel güncelleme.
/// P1: Ürün görseli pazaryeri listelemeleri için kritik.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateProductImageHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateProductImageHandler CreateSut() => new(_productRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        var cmd = new UpdateProductImageCommand(Guid.NewGuid(), "https://cdn.test.com/img.jpg");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldUpdateImageUrl()
    {
        var product = FakeData.CreateProduct();
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var newUrl = "https://cdn.mestech.com/products/new-image.webp";
        var cmd = new UpdateProductImageCommand(product.Id, newUrl);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.ImageUrl.Should().Be(newUrl);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
