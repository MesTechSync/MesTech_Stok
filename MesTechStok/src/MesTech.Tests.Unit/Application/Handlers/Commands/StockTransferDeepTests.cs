using FluentAssertions;
using MesTech.Application.Commands.TransferStock;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// StockTransfer derin testler — kaynak düşer, hedef artar, hareket kaydı oluşur.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class StockTransferDeepTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    public StockTransferDeepTests()
    {
        _tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private TransferStockHandler CreateSut() => new(
        _productRepo.Object, _movementRepo.Object, _warehouseRepo.Object,
        _uow.Object, _tenantProvider.Object);

    private static Warehouse MakeWH(string name) => new() { Name = name, TenantId = Guid.NewGuid() };

    [Fact]
    public async Task Transfer_SourceStockDecreases_TargetStockIncreases()
    {
        var product = FakeData.CreateProduct(stock: 80);
        var source = MakeWH("Kaynak");
        var target = MakeWH("Hedef");

        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _warehouseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == source.Id ? source : target);

        var cmd = new TransferStockCommand(product.Id, source.Id, target.Id, 25);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SourceRemainingStock.Should().Be(55, "80 - 25 = 55");
        result.TargetNewStock.Should().Be(25, "0 + 25 = 25");
    }

    [Fact]
    public async Task Transfer_AllStock_SourceBecomesZero()
    {
        var product = FakeData.CreateProduct(stock: 30);
        var source = MakeWH("Full");
        var target = MakeWH("Empty");

        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _warehouseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == source.Id ? source : target);

        var cmd = new TransferStockCommand(product.Id, source.Id, target.Id, 30);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SourceRemainingStock.Should().Be(0, "tüm stok transfer edildi");
        result.TargetNewStock.Should().Be(30);
    }

    [Fact]
    public async Task Transfer_NegativeQuantity_ShouldReturnError()
    {
        var cmd = new TransferStockCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -5);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Transfer_WarehouseNotFound_ShouldReturnError()
    {
        var product = FakeData.CreateProduct(stock: 50);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _warehouseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var cmd = new TransferStockCommand(product.Id, Guid.NewGuid(), Guid.NewGuid(), 10);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Transfer_ExactStock_ShouldCreateTwoMovements()
    {
        var product = FakeData.CreateProduct(stock: 50);
        var source = MakeWH("S");
        var target = MakeWH("T");

        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _warehouseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == source.Id ? source : target);

        var cmd = new TransferStockCommand(product.Id, source.Id, target.Id, 15);
        await CreateSut().Handle(cmd, CancellationToken.None);

        // TransferOut + TransferIn = 2 hareket
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Exactly(2),
            "transfer should create both TransferOut and TransferIn movements");
    }
}
