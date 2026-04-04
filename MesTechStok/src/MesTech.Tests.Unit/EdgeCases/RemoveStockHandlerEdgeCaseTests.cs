using FluentAssertions;
using MesTech.Application.Commands.RemoveStock;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.EdgeCases;

/// <summary>
/// RemoveStockHandler edge cases — null guards, boundary quantities, constructor validation.
/// Dalga 14 — DEV 5.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga14")]
public class RemoveStockHandlerEdgeCaseTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly StockCalculationService _stockCalc = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private RemoveStockHandler CreateHandler() =>
        new(_productRepo.Object, _movementRepo.Object, _unitOfWork.Object, _stockCalc, _tenantProvider.Object);

    // ── Constructor Null Guards ──

    [Fact]
    public void Constructor_NullProductRepo_ShouldThrow()
    {
        var act = () => new RemoveStockHandler(
            null!, _movementRepo.Object, _unitOfWork.Object, _stockCalc, _tenantProvider.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("productRepository");
    }

    [Fact]
    public void Constructor_NullMovementRepo_ShouldThrow()
    {
        var act = () => new RemoveStockHandler(
            _productRepo.Object, null!, _unitOfWork.Object, _stockCalc, _tenantProvider.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("movementRepository");
    }

    [Fact]
    public void Constructor_NullUnitOfWork_ShouldThrow()
    {
        var act = () => new RemoveStockHandler(
            _productRepo.Object, _movementRepo.Object, null!, _stockCalc, _tenantProvider.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("unitOfWork");
    }

    [Fact]
    public void Constructor_NullStockCalc_ShouldThrow()
    {
        var act = () => new RemoveStockHandler(
            _productRepo.Object, _movementRepo.Object, _unitOfWork.Object, null!, _tenantProvider.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("stockCalc");
    }

    // ── Handle Null Request Guard ──

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = CreateHandler();

        var act = () => handler.Handle(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── Edge Case Scenarios ──

    [Fact]
    public async Task Handle_RemoveZeroQuantity_ShouldSucceedWithNoChange()
    {
        var product = FakeData.CreateProduct(stock: 50);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new RemoveStockCommand(product.Id, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(50);
    }

    [Fact]
    public async Task Handle_ShouldNotCallSaveChanges_WhenProductNotFound()
    {
        var id = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Product?)null);

        var handler = CreateHandler();
        var command = new RemoveStockCommand(id, 10);

        await handler.Handle(command, CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ShouldNotModifyProduct()
    {
        var product = FakeData.CreateProduct(sku: "GUARD-01", stock: 5);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new RemoveStockCommand(product.Id, 100);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        product.Stock.Should().Be(5); // Unchanged
        _productRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDocumentNumber_ShouldCaptureInMovement()
    {
        var product = FakeData.CreateProduct(stock: 100);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        StockMovement? captured = null;
        _movementRepo.Setup(r => r.AddAsync(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(m => captured = m);

        var handler = CreateHandler();
        var command = new RemoveStockCommand(product.Id, 10,
            Reason: "Iade", DocumentNumber: "RET-001");

        await handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.DocumentNumber.Should().Be("RET-001");
        captured.Reason.Should().Be("Iade");
        captured.MovementType.Should().Be("StockOut");
    }

    [Fact]
    public async Task Handle_CancellationToken_ShouldBePassedToSaveChanges()
    {
        var product = FakeData.CreateProduct(stock: 50);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var cts = new CancellationTokenSource();
        var handler = CreateHandler();
        var command = new RemoveStockCommand(product.Id, 5);

        await handler.Handle(command, cts.Token);

        _unitOfWork.Verify(u => u.SaveChangesAsync(cts.Token), Times.Once);
    }
}
