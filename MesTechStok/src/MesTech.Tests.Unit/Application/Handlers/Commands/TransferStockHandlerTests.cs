using FluentAssertions;
using MesTech.Application.Commands.TransferStock;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: TransferStockHandler testi — depo-arası stok transfer.
/// P1 iş-kritik: yanlış transfer = stok tutarsızlığı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class TransferStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private TransferStockHandler CreateSut()
    {
        _tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        return new(_productRepo.Object, _movementRepo.Object, _warehouseRepo.Object, _uow.Object, _tenantProvider.Object);
    }

    private static Warehouse CreateWarehouse(string name = "WH-1") =>
        new() { Name = name, TenantId = Guid.NewGuid() };

    [Fact]
    public async Task Handle_HappyPath_ShouldReturnSuccessWithCorrectStockLevels()
    {
        var product = FakeData.CreateProduct(stock: 100);
        var source = CreateWarehouse("Source");
        var target = CreateWarehouse("Target");

        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _warehouseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == source.Id ? source : target);

        var cmd = new TransferStockCommand(product.Id, source.Id, target.Id, 30);
        var sut = CreateSut();
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SourceRemainingStock.Should().Be(70);
        result.TargetNewStock.Should().Be(30);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ZeroQuantity_ShouldReturnError()
    {
        var cmd = new TransferStockCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0);
        var sut = CreateSut();
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("pozitif");
    }

    [Fact]
    public async Task Handle_SameSourceAndTarget_ShouldReturnError()
    {
        var warehouseId = Guid.NewGuid();
        var cmd = new TransferStockCommand(Guid.NewGuid(), warehouseId, warehouseId, 10);
        var sut = CreateSut();
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("aynı");
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnError()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        var cmd = new TransferStockCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10);
        var sut = CreateSut();
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_InsufficientStock_ShouldReturnError()
    {
        var product = FakeData.CreateProduct(stock: 5);
        var source = CreateWarehouse();
        var target = CreateWarehouse();

        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _warehouseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == source.Id ? source : target);

        var cmd = new TransferStockCommand(product.Id, source.Id, target.Id, 50);
        var sut = CreateSut();
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Yetersiz");
    }

    [Fact]
    public async Task Handle_ShouldCreateTwoMovements_OutAndIn()
    {
        var product = FakeData.CreateProduct(stock: 100);
        var source = CreateWarehouse("Source");
        var target = CreateWarehouse("Target");

        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _warehouseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == source.Id ? source : target);

        var cmd = new TransferStockCommand(product.Id, source.Id, target.Id, 20);
        var sut = CreateSut();
        await sut.Handle(cmd, CancellationToken.None);

        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
