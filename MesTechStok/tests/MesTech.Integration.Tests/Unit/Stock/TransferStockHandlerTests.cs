using FluentAssertions;
using MesTech.Application.Commands.TransferStock;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// TransferStockHandler: depo arası stok transferi.
/// Kritik iş kuralları:
///   - Aynı depo arası transfer red edilmeli
///   - Yetersiz stok kontrolü
///   - 2 atomik StockMovement (IN + OUT) oluşturulmalı
///   - Sıfır/negatif miktar red edilmeli
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "StockMovement")]
public class TransferStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public TransferStockHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _movementRepo.Setup(r => r.AddAsync(It.IsAny<StockMovement>())).Returns(Task.CompletedTask);
    }

    private TransferStockHandler CreateHandler() =>
        new(_productRepo.Object, _movementRepo.Object, _warehouseRepo.Object, _uow.Object);

    private Product CreateProduct(int stock = 100) =>
        new() { Name = "Transfer Ürün", SKU = "TRF-001", Stock = stock, CategoryId = Guid.NewGuid() };

    private Warehouse CreateWarehouse(string name) =>
        new() { Name = name };

    [Fact]
    public async Task Handle_ValidTransfer_CreatesDoubleMovement()
    {
        var product = CreateProduct(50);
        var source = CreateWarehouse("Ana Depo");
        var target = CreateWarehouse("Şube Depo");

        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _warehouseRepo.Setup(r => r.GetByIdAsync(source.Id)).ReturnsAsync(source);
        _warehouseRepo.Setup(r => r.GetByIdAsync(target.Id)).ReturnsAsync(target);

        var cmd = new TransferStockCommand(product.Id, source.Id, target.Id, 20, "Şube ihtiyacı");
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SourceRemainingStock.Should().Be(30); // 50 - 20
        result.TargetNewStock.Should().Be(20);
        // 2 movement kaydedilmeli (IN + OUT)
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Exactly(2));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SameWarehouse_ReturnsFailure()
    {
        var warehouseId = Guid.NewGuid();
        var cmd = new TransferStockCommand(Guid.NewGuid(), warehouseId, warehouseId, 10);
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("aynı");
    }

    [Fact]
    public async Task Handle_ZeroQuantity_ReturnsFailure()
    {
        var cmd = new TransferStockCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0);
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("pozitif");
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsFailure()
    {
        var product = CreateProduct(5);
        var source = CreateWarehouse("Kaynak");
        var target = CreateWarehouse("Hedef");

        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _warehouseRepo.Setup(r => r.GetByIdAsync(source.Id)).ReturnsAsync(source);
        _warehouseRepo.Setup(r => r.GetByIdAsync(target.Id)).ReturnsAsync(target);

        var cmd = new TransferStockCommand(product.Id, source.Id, target.Id, 20);
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Yetersiz");
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var cmd = new TransferStockCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10);
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }
}
