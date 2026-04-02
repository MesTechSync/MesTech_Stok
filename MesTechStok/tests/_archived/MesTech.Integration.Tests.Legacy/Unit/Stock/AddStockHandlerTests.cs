using FluentAssertions;
using MesTech.Application.Commands.AddStock;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// AddStockHandler: stok girişi + StockMovement audit kaydı.
/// StockChangedEvent → platform sync tetikler.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "StockMovement")]
public class AddStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public AddStockHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _movementRepo.Setup(r => r.AddAsync(It.IsAny<StockMovement>())).Returns(Task.CompletedTask);
    }

    private AddStockHandler CreateHandler() =>
        new(_productRepo.Object, _movementRepo.Object, _uow.Object);

    private Product CreateProduct(int initialStock = 0)
    {
        var product = new Product { Name = "Test Ürün", SKU = "TST-001", PurchasePrice = 50m, SalePrice = 100m, CategoryId = Guid.NewGuid() };
        if (initialStock > 0)
            product.AdjustStock(initialStock, StockMovementType.StockIn);
        return product;
    }

    [Fact]
    public async Task Handle_ValidAddStock_IncrementsStock_And_RecordsMovement()
    {
        // Arrange
        var product = CreateProduct(initialStock: 20);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cmd = new AddStockCommand(
            ProductId: product.Id,
            Quantity: 30,
            UnitCost: 45m,
            BatchNumber: "BATCH-2026-001",
            DocumentNumber: "PO-001",
            Reason: "Tedarikçi siparişi");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(50); // 20 + 30
        product.Stock.Should().Be(50);
        _movementRepo.Verify(r => r.AddAsync(It.Is<StockMovement>(m =>
            m.ProductId == product.Id &&
            m.Quantity == 30)), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Product?)null);

        var cmd = new AddStockCommand(missingId, 10, 50m);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AddToZeroStock_ProductNoLongerOutOfStock()
    {
        // Arrange — stok 0'dan başlıyor
        var product = CreateProduct(initialStock: 0);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        product.IsOutOfStock().Should().BeTrue(); // precondition

        var cmd = new AddStockCommand(product.Id, 100, 50m);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(100);
        product.IsOutOfStock().Should().BeFalse();
    }

    [Fact]
    public async Task Handle_StockMovement_CapturesCostBasis()
    {
        // Arrange
        var product = CreateProduct(initialStock: 10);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        StockMovement? capturedMovement = null;
        _movementRepo.Setup(r => r.AddAsync(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(m => capturedMovement = m)
            .Returns(Task.CompletedTask);

        var cmd = new AddStockCommand(
            ProductId: product.Id,
            Quantity: 25,
            UnitCost: 60m,
            BatchNumber: "LOT-A",
            DocumentNumber: "INV-123");

        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert — maliyet takibi doğru mu?
        capturedMovement.Should().NotBeNull();
        capturedMovement!.Quantity.Should().Be(25);
        capturedMovement.UnitCost.Should().Be(60m);
        capturedMovement.TotalCost.Should().Be(1500m); // 25 × 60
    }
}
