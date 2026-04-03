using FluentAssertions;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Application.Commands.RemoveStock;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

/// <summary>
/// DEV 5 — Dalga 7 Task 5.14: Application coverage gap-fill.
/// Edge cases for PlaceOrder, RemoveStock, SyncPlatform handlers.
/// Targets untested code paths: null request, empty items, partial failures,
/// domain event propagation, SaveChanges failures.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "AppGapFill")]
public class PlaceOrderEdgeCaseTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly StockCalculationService _stockCalc = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private PlaceOrderHandler CreateHandler() =>
        new(_orderRepo.Object, _productRepo.Object, _unitOfWork.Object, _stockCalc, _tenantProvider.Object);

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_EmptyItemsList_CreatesOrderWithNoItems()
    {
        // Empty items list: foreach runs 0 iterations, order has no items
        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Test Customer", null, null,
            new List<PlaceOrderItem>());

        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.OrderNumber.Should().StartWith("ORD-");
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleItems_SecondProductNotFound_ReturnsError()
    {
        var product1 = FakeData.CreateProduct(sku: "P1", stock: 100, salePrice: 50m);
        var product2Id = Guid.NewGuid();

        // Handler uses GetByIdsAsync (batch) — only product1 is returned, product2 missing
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1 });

        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Customer", null, null,
            new List<PlaceOrderItem>
            {
                new(product1.Id, 2, 50m),
                new(product2Id, 1, 30m)
            });

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(product2Id.ToString());
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ThrowsFromDomainService()
    {
        var product = FakeData.CreateProduct(sku: "LOW-STOCK", stock: 2, salePrice: 100m);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Customer", null, null,
            new List<PlaceOrderItem> { new(product.Id, 100, 100m) });

        var handler = CreateHandler();

        var act = () => handler.Handle(command, CancellationToken.None);

        // StockCalculationService.ValidateStockSufficiency throws InsufficientStockException
        await act.Should().ThrowAsync<InsufficientStockException>();
    }

    [Fact]
    public async Task Handle_ValidOrder_SetsCustomerInfoOnOrder()
    {
        var customerId = Guid.NewGuid();
        var product = FakeData.CreateProduct(sku: "CUST-TEST", stock: 50, salePrice: 100m);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        Order? capturedOrder = null;
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o);

        var command = new PlaceOrderCommand(
            customerId, "Ahmet Kaya", "ahmet@test.com", "Acil siparis",
            new List<PlaceOrderItem> { new(product.Id, 1, 100m) });

        var handler = CreateHandler();
        await handler.Handle(command, CancellationToken.None);

        capturedOrder.Should().NotBeNull();
        capturedOrder!.CustomerId.Should().Be(customerId);
        capturedOrder.CustomerName.Should().Be("Ahmet Kaya");
        capturedOrder.CustomerEmail.Should().Be("ahmet@test.com");
        capturedOrder.Notes.Should().Be("Acil siparis");
    }

    [Fact]
    public async Task Handle_SingleItem_DeductsStockCorrectly()
    {
        var product = FakeData.CreateProduct(sku: "DEDUCT-TEST", stock: 50, salePrice: 100m);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Test", null, null,
            new List<PlaceOrderItem> { new(product.Id, 10, 100m) });

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(50, "Stock deduction is now handled by OrderPlacedStockDeductionHandler (Z1 chain), not PlaceOrderHandler");
    }

    [Fact]
    public async Task Handle_OrderNumberFormat_ContainsDateAndGuidPrefix()
    {
        var product = FakeData.CreateProduct(stock: 10);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        Order? capturedOrder = null;
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o);

        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Test", null, null,
            new List<PlaceOrderItem> { new(product.Id, 1, 50m) });

        var handler = CreateHandler();
        await handler.Handle(command, CancellationToken.None);

        capturedOrder.Should().NotBeNull();
        capturedOrder!.OrderNumber.Should().StartWith("ORD-");
        capturedOrder.OrderNumber.Should().Contain(DateTime.UtcNow.ToString("yyyyMMdd"));
    }

    [Fact]
    public void PlaceOrderHandler_NullStockCalc_ThrowsArgumentNullException()
    {
        var act = () => new PlaceOrderHandler(
            _orderRepo.Object, _productRepo.Object, _unitOfWork.Object, null!, _tenantProvider.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stockCalculation");
    }
}

[Trait("Category", "Unit")]
[Trait("Category", "AppGapFill")]
public class RemoveStockEdgeCaseTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly StockCalculationService _stockCalc = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private RemoveStockHandler CreateHandler() =>
        new(_productRepo.Object, _movementRepo.Object, _unitOfWork.Object, _stockCalc, _tenantProvider.Object);

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ValidRemoval_RaisesDomainEvent()
    {
        var product = FakeData.CreateProduct(sku: "EVT-RM", stock: 20, salePrice: 100m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new RemoveStockCommand(product.Id, 5, "Test removal");

        await handler.Handle(command, CancellationToken.None);

        product.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_MovementCreated_HasCorrectFields()
    {
        var product = FakeData.CreateProduct(sku: "MVT-CHK", stock: 30, salePrice: 50m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        StockMovement? captured = null;
        _movementRepo.Setup(r => r.AddAsync(It.IsAny<StockMovement>()))
            .Callback<StockMovement>(m => captured = m);

        var handler = CreateHandler();
        var command = new RemoveStockCommand(product.Id, 10, "Defective batch", "DOC-001");

        await handler.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Quantity.Should().Be(-10);
        captured.PreviousStock.Should().Be(30);
        captured.NewStock.Should().Be(20);
        captured.Reason.Should().Be("Defective batch");
        captured.DocumentNumber.Should().Be("DOC-001");
    }

    [Fact]
    public async Task Handle_ExactStockRemoval_ResultsInZeroStock()
    {
        var product = FakeData.CreateProduct(sku: "EXACT-RM", stock: 15, salePrice: 100m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RemoveStockCommand(product.Id, 15, "Sold out"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.NewStockLevel.Should().Be(0);
        product.Stock.Should().Be(0);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsError_DoesNotSave()
    {
        var product = FakeData.CreateProduct(sku: "INS-RM", stock: 5, salePrice: 100m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RemoveStockCommand(product.Id, 100, "Too much"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _movementRepo.Verify(r => r.AddAsync(It.IsAny<StockMovement>()), Times.Never);
    }

    [Fact]
    public void RemoveStockHandler_NullProductRepo_ThrowsArgumentNullException()
    {
        var act = () => new RemoveStockHandler(
            null!, _movementRepo.Object, _unitOfWork.Object, _stockCalc, _tenantProvider.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("productRepository");
    }

    [Fact]
    public void RemoveStockHandler_NullStockCalc_ThrowsArgumentNullException()
    {
        var act = () => new RemoveStockHandler(
            _productRepo.Object, _movementRepo.Object, _unitOfWork.Object, null!, _tenantProvider.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stockCalc");
    }
}

[Trait("Category", "Unit")]
[Trait("Category", "AppGapFill")]
public class SyncPlatformEdgeCaseTests
{
    private readonly Mock<IIntegratorOrchestrator> _orchestrator = new();

    private SyncPlatformHandler CreateHandler() => new(_orchestrator.Object);

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WildcardPlatformCode_CallsSyncAllPlatforms()
    {
        _orchestrator.Setup(o => o.SyncAllPlatformsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResultDto
            {
                IsSuccess = true,
                ItemsProcessed = 10,
                ItemsFailed = 0
            });

        var handler = CreateHandler();
        var command = new SyncPlatformCommand("*", SyncDirection.Bidirectional);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ItemsProcessed.Should().Be(10);
        _orchestrator.Verify(o => o.SyncAllPlatformsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orchestrator.Verify(o => o.SyncPlatformAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SpecificPlatform_CallsSyncPlatformWithCode()
    {
        _orchestrator.Setup(o => o.SyncPlatformAsync("Trendyol", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResultDto
            {
                IsSuccess = true,
                ItemsProcessed = 5,
                ItemsFailed = 1
            });

        var handler = CreateHandler();
        var command = new SyncPlatformCommand("Trendyol", SyncDirection.Push);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ItemsProcessed.Should().Be(5);
        result.ItemsFailed.Should().Be(1);
        _orchestrator.Verify(o => o.SyncPlatformAsync("Trendyol", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrchestratorThrows_ExceptionPropagates()
    {
        _orchestrator.Setup(o => o.SyncPlatformAsync("BadPlatform", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unknown platform"));

        var handler = CreateHandler();
        var command = new SyncPlatformCommand("BadPlatform", SyncDirection.Pull);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unknown platform");
    }

    [Fact]
    public async Task Handle_FailedSync_ReturnsErrorResult()
    {
        _orchestrator.Setup(o => o.SyncPlatformAsync("OpenCart", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResultDto
            {
                IsSuccess = false,
                ErrorMessage = "API timeout"
            });

        var handler = CreateHandler();
        var result = await handler.Handle(
            new SyncPlatformCommand("OpenCart", SyncDirection.Pull), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void SyncPlatformHandler_NullOrchestrator_ThrowsArgumentNullException()
    {
        var act = () => new SyncPlatformHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
