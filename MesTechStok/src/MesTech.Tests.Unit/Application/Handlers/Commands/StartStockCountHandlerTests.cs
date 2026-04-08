using FluentAssertions;
using MesTech.Application.Features.Stock.Commands.StartStockCount;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: StartStockCountHandler testi — stok sayım oturumu başlatma.
/// P1: Stok sayım = envanter doğruluğunun temelidir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class StartStockCountHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();

    private StartStockCountHandler CreateSut() => new(_productRepo.Object, _warehouseRepo.Object);

    [Fact]
    public async Task Handle_NoProducts_ShouldReturnEmptySession()
    {
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var cmd = new StartStockCountCommand(Guid.NewGuid());
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.SessionId.Should().NotBeEmpty();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithProducts_ShouldMapAllToCountItems()
    {
        var products = new List<Product>
        {
            FakeData.CreateProduct(sku: "SKU-A", stock: 100),
            FakeData.CreateProduct(sku: "SKU-B", stock: 50),
        };

        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var cmd = new StartStockCountCommand(Guid.NewGuid());
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].SKU.Should().Be("SKU-A");
        result.Items[0].ExpectedStock.Should().Be(100);
        result.Items[0].CountedStock.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithWarehouse_ShouldSetWarehouseName()
    {
        var wh = new Warehouse { Name = "Ana Depo", TenantId = Guid.NewGuid() };
        _warehouseRepo.Setup(r => r.GetByIdAsync(wh.Id, It.IsAny<CancellationToken>())).ReturnsAsync(wh);
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var cmd = new StartStockCountCommand(Guid.NewGuid(), wh.Id);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.WarehouseName.Should().Be("Ana Depo");
    }

    [Fact]
    public async Task Handle_NoWarehouse_ShouldHaveNullWarehouseName()
    {
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var cmd = new StartStockCountCommand(Guid.NewGuid());
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.WarehouseName.Should().BeNull();
    }
}
