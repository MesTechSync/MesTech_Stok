using FluentAssertions;
using MesTech.Application.Commands.AddStock;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

public class AddStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Handle_ValidCommand_ShouldIncreaseStock()
    {
        var product = FakeData.CreateProduct(sku: "TEST-001", stock: 100);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var handler = new AddStockHandler(_productRepo.Object, _movementRepo.Object, _unitOfWork.Object);
        var command = new AddStockCommand(product.Id, 50, 25.00m, Reason: "Test ekleme");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(150);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnError()
    {
        _productRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

        var handler = new AddStockHandler(_productRepo.Object, _movementRepo.Object, _unitOfWork.Object);
        var command = new AddStockCommand(999, 50, 10.00m, Reason: "Test");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("999");
    }

    [Fact]
    public async Task Handle_ShouldCreateStockMovement()
    {
        var product = FakeData.CreateProduct(sku: "MOV-001", stock: 50);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var handler = new AddStockHandler(_productRepo.Object, _movementRepo.Object, _unitOfWork.Object);
        var command = new AddStockCommand(product.Id, 30, 15.00m, Reason: "Stok giris");

        await handler.Handle(command, CancellationToken.None);

        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
