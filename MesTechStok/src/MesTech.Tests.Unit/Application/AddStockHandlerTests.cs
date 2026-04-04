using FluentAssertions;
using MesTech.Application.Commands.AddStock;
using MesTech.Domain.Entities;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class AddStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private AddStockHandler CreateHandler() =>
        new(_productRepo.Object, _movementRepo.Object, _unitOfWork.Object, _tenantProvider.Object);

    [Fact]
    public async Task Handle_ValidCommand_ShouldIncreaseStock()
    {
        var product = FakeData.CreateProduct(sku: "TEST-001", stock: 100);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new AddStockCommand(product.Id, 50, 25.00m, Reason: "Test ekleme");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(150);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnError()
    {
        var nonExistentId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = CreateHandler();
        var command = new AddStockCommand(nonExistentId, 50, 10.00m, Reason: "Test");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(nonExistentId.ToString());
    }

    [Fact]
    public async Task Handle_ShouldCreateStockMovement()
    {
        var product = FakeData.CreateProduct(sku: "MOV-001", stock: 50);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new AddStockCommand(product.Id, 30, 15.00m, Reason: "Stok giris");

        await handler.Handle(command, CancellationToken.None);

        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── DEV5 Dalga 1: Expanded Handler Tests ──

    [Fact]
    public async Task Handle_NegativeQuantity_ShouldDecreaseStock()
    {
        var product = FakeData.CreateProduct(sku: "NEG-H01", stock: 100);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new AddStockCommand(product.Id, -30, 10.00m, Reason: "Stok cikis");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(70);
    }

    [Fact]
    public async Task Handle_ZeroQuantity_ShouldNotChangeStock()
    {
        var product = FakeData.CreateProduct(sku: "ZERO-001", stock: 50);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new AddStockCommand(product.Id, 0, 0m, Reason: "Sifir hareket");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(50);
    }

    [Fact]
    public async Task Handle_WithBatchAndExpiry_ShouldPassToMovement()
    {
        var product = FakeData.CreateProduct(sku: "BATCH-001", stock: 20);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var expiry = new DateTime(2027, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        StockMovement? captured = null;
        _movementRepo
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m);

        var handler = CreateHandler();
        var command = new AddStockCommand(product.Id, 100, 5.00m,
            BatchNumber: "LOT-2026-03", ExpiryDate: expiry, DocumentNumber: "GRS-001");

        await handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.BatchNumber.Should().Be("LOT-2026-03");
        captured.ExpiryDate.Should().Be(expiry);
        captured.DocumentNumber.Should().Be("GRS-001");
    }

    [Fact]
    public async Task Handle_ShouldSetCorrectUnitAndTotalCost()
    {
        var product = FakeData.CreateProduct(sku: "COST-001", stock: 0);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        StockMovement? captured = null;
        _movementRepo
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m);

        var handler = CreateHandler();
        var command = new AddStockCommand(product.Id, 25, 12.50m);

        await handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.UnitCost.Should().Be(12.50m);
        captured.TotalCost.Should().Be(25 * 12.50m); // 312.50
    }

    [Fact]
    public async Task Handle_LargeQuantity_ShouldHandleCorrectly()
    {
        var product = FakeData.CreateProduct(sku: "BIG-001", stock: 0);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new AddStockCommand(product.Id, 100_000, 0.50m, Reason: "Toplu giris");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(100_000);
    }

    [Fact]
    public async Task Handle_ShouldRaiseDomainEvent()
    {
        var product = FakeData.CreateProduct(sku: "EVT-001", stock: 40);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new AddStockCommand(product.Id, 10, 5.00m);

        await handler.Handle(command, CancellationToken.None);

        // AdjustStock raises StockChangedEvent inside Product domain
        product.DomainEvents.Should().ContainSingle();
        var domainEvent = product.DomainEvents[0] as StockChangedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.PreviousQuantity.Should().Be(40);
        domainEvent.NewQuantity.Should().Be(50);
    }
}
