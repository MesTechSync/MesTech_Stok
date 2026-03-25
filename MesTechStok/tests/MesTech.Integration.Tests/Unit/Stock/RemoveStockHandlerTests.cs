using FluentAssertions;
using MesTech.Application.Commands.RemoveStock;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// RemoveStockHandler: stok çıkışı + yetersiz stok koruması.
/// InsufficientStockException → graceful error result.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "StockMovement")]
public class RemoveStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly StockCalculationService _stockCalc = new();

    public RemoveStockHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _movementRepo.Setup(r => r.AddAsync(It.IsAny<StockMovement>())).Returns(Task.CompletedTask);
    }

    private RemoveStockHandler CreateHandler() =>
        new(_productRepo.Object, _movementRepo.Object, _uow.Object, _stockCalc);

    private Product CreateProduct(int initialStock = 100)
    {
        var product = new Product { Name = "Test Ürün", SKU = "TST-001", PurchasePrice = 50m, SalePrice = 100m, CategoryId = Guid.NewGuid() };
        if (initialStock > 0)
            product.AdjustStock(initialStock, StockMovementType.StockIn);
        return product;
    }

    [Fact]
    public async Task Handle_ValidRemoval_DecrementsStock_And_RecordsMovement()
    {
        // Arrange
        var product = CreateProduct(initialStock: 50);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cmd = new RemoveStockCommand(
            ProductId: product.Id,
            Quantity: 15,
            Reason: "Hasar tespit",
            DocumentNumber: "DMG-001");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(35); // 50 - 15
        product.Stock.Should().Be(35);
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsFailure_NoStockChange()
    {
        // Arrange — 10 stokta, 20 çıkmak istiyor
        var product = CreateProduct(initialStock: 10);
        var originalStock = product.Stock;
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cmd = new RemoveStockCommand(product.Id, Quantity: 20, Reason: "Sipariş");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert — stok DEĞİŞMEMELİ
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        product.Stock.Should().Be(originalStock); // stok korundu
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Product?)null);

        var cmd = new RemoveStockCommand(missingId, 5);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RemoveExactStock_ResultsInZero()
    {
        // Arrange — tam stok kadar çıkar
        var product = CreateProduct(initialStock: 25);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cmd = new RemoveStockCommand(product.Id, Quantity: 25, Reason: "Tam tüketim");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(0);
        product.IsOutOfStock().Should().BeTrue();
    }
}
