using FluentAssertions;
using MesTech.Application.Commands.DeleteProduct;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class DeleteProductHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteProductHandler CreateHandler() =>
        new(_productRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_ExistingProduct_ShouldDeleteSuccessfully()
    {
        var product = FakeData.CreateProduct(sku: "DEL-001");
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new DeleteProductCommand(product.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _productRepo.Verify(r => r.DeleteAsync(product.Id), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnError()
    {
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Product?)null);

        var handler = CreateHandler();
        var command = new DeleteProductCommand(missingId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        _productRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallDeleteWithCorrectId()
    {
        var product = FakeData.CreateProduct(sku: "DEL-VERIFY");
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        Guid? capturedId = null;
        _productRepo.Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
            .Callback<Guid>(id => capturedId = id);

        var handler = CreateHandler();
        await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        capturedId.Should().Be(product.Id);
    }

    [Fact]
    public async Task Handle_NotFound_ShouldNotCallUnitOfWork()
    {
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Product?)null);

        var handler = CreateHandler();
        await handler.Handle(new DeleteProductCommand(missingId), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MultipleDeletes_ShouldWorkIndependently()
    {
        var p1 = FakeData.CreateProduct(sku: "DEL-A");
        var p2 = FakeData.CreateProduct(sku: "DEL-B");
        _productRepo.Setup(r => r.GetByIdAsync(p1.Id)).ReturnsAsync(p1);
        _productRepo.Setup(r => r.GetByIdAsync(p2.Id)).ReturnsAsync(p2);

        var handler = CreateHandler();

        var r1 = await handler.Handle(new DeleteProductCommand(p1.Id), CancellationToken.None);
        var r2 = await handler.Handle(new DeleteProductCommand(p2.Id), CancellationToken.None);

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();
        _productRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Exactly(2));
    }
}
