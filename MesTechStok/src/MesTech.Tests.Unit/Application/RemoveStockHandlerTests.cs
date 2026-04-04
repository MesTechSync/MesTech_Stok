using FluentAssertions;
using MesTech.Application.Commands.RemoveStock;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class RemoveStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly StockCalculationService _stockCalc = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private RemoveStockHandler CreateHandler() =>
        new(_productRepo.Object, _movementRepo.Object, _unitOfWork.Object, _stockCalc, _tenantProvider.Object);

    [Fact]
    public async Task Handle_ValidRemoval_ShouldDecreaseStock()
    {
        var product = FakeData.CreateProduct(sku: "REM-001", stock: 100);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new RemoveStockCommand(product.Id, 30, Reason: "Satis");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(70);
        result.MovementId.Should().NotBeEmpty();
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnError()
    {
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var handler = CreateHandler();
        var command = new RemoveStockCommand(missingId, 10);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
    }

    [Fact]
    public async Task Handle_InsufficientStock_ShouldReturnError()
    {
        var product = FakeData.CreateProduct(sku: "INSUF-001", stock: 5);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new RemoveStockCommand(product.Id, 50);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Insufficient stock");
    }

    [Fact]
    public async Task Handle_ExactStockRemoval_ShouldResultInZero()
    {
        var product = FakeData.CreateProduct(sku: "EXACT-001", stock: 25);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new RemoveStockCommand(product.Id, 25, Reason: "Tam cikis");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldCreateCorrectMovementRecord()
    {
        var product = FakeData.CreateProduct(sku: "MOV-REM-01", stock: 80);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        StockMovement? captured = null;
        _movementRepo.Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m);

        var handler = CreateHandler();
        var command = new RemoveStockCommand(product.Id, 20, Reason: "Hasar", DocumentNumber: "DMG-001");

        await handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Quantity.Should().Be(-20);
        captured.PreviousStock.Should().Be(80);
        captured.NewStock.Should().Be(60);
        captured.Reason.Should().Be("Hasar");
        captured.DocumentNumber.Should().Be("DMG-001");
    }
}
