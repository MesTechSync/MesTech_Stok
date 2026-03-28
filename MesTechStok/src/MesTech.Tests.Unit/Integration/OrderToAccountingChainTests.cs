using FluentAssertions;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Integration;

/// <summary>
/// Order-to-Accounting chain integration tests.
/// Verifies the full business chain:
///   PlaceOrder → OrderConfirmedRevenue → CommissionChargedGL → OrderShippedCost
/// Each handler's output is validated and used as input for the next step.
/// </summary>
[Trait("Category", "Unit")]
public class OrderToAccountingChainTests
{
    private readonly Mock<IOrderRepository> _orderRepo;
    private readonly Mock<IProductRepository> _productRepo;
    private readonly Mock<IIncomeRepository> _incomeRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly StockCalculationService _stockCalc;

    private readonly PlaceOrderHandler _placeOrderHandler;
    private readonly OrderConfirmedRevenueHandler _revenueHandler;
    private readonly CommissionChargedGLHandler _commissionHandler;
    private readonly OrderShippedCostHandler _shippedCostHandler;

    // Captured entities for chain verification
    private Order? _capturedOrder;
    private Income? _capturedIncome;

    public OrderToAccountingChainTests()
    {
        _orderRepo = new Mock<IOrderRepository>();
        _productRepo = new Mock<IProductRepository>();
        _incomeRepo = new Mock<IIncomeRepository>();
        _uow = new Mock<IUnitOfWork>();
        _stockCalc = new StockCalculationService();

        // Capture order on AddAsync
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => _capturedOrder = o)
            .Returns(Task.CompletedTask);

        // Capture income on AddAsync
        _incomeRepo.Setup(r => r.AddAsync(It.IsAny<Income>()))
            .Callback<Income>(i => _capturedIncome = i)
            .Returns(Task.CompletedTask);

        _placeOrderHandler = new PlaceOrderHandler(
            _orderRepo.Object, _productRepo.Object, _uow.Object, _stockCalc);

        _revenueHandler = new OrderConfirmedRevenueHandler(
            _incomeRepo.Object, _uow.Object,
            NullLogger<OrderConfirmedRevenueHandler>.Instance);

        _commissionHandler = new CommissionChargedGLHandler(
            _uow.Object, Mock.Of<IJournalEntryRepository>(), NullLogger<CommissionChargedGLHandler>.Instance);

        _shippedCostHandler = new OrderShippedCostHandler(
            _uow.Object, Mock.Of<IJournalEntryRepository>(), NullLogger<OrderShippedCostHandler>.Instance);
    }

    /// <summary>
    /// Creates a product and registers it in the mock repository.
    /// Returns the product so callers can use product.Id for command items.
    /// </summary>
    private Product CreateAndRegisterProduct(string sku, int stock, decimal price)
    {
        var product = new Product
        {
            SKU = sku,
            Name = $"Test Product {sku}",
            Stock = stock,
            SalePrice = price,
            PurchasePrice = price * 0.6m,
            CategoryId = Guid.NewGuid()
        };
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);
        return product;
    }

    [Fact]
    public async Task FullChain_PlaceOrder_ThenRevenue_ThenCommission_ThenShipping_AllGLEntriesCreated()
    {
        // Arrange — product setup
        var product = CreateAndRegisterProduct("TST-001", 100, 250m);

        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var command = new PlaceOrderCommand(
            customerId, "Test Customer", "test@example.com", "Chain test",
            new List<PlaceOrderItem>
            {
                new(product.Id, 2, 250m, 0.18m)
            });

        // === STEP 1: PlaceOrder ===
        var orderResult = await _placeOrderHandler.Handle(command, CancellationToken.None);

        orderResult.IsSuccess.Should().BeTrue("PlaceOrder should succeed");
        _capturedOrder.Should().NotBeNull("Order should be persisted");
        _capturedOrder!.Status.Should().Be(OrderStatus.Confirmed, "Order should be Confirmed after Place()");
        _capturedOrder.TotalAmount.Should().BeGreaterThan(0, "Order total should be positive");
        _capturedOrder.OrderItems.Should().HaveCount(1);

        var orderId = _capturedOrder.Id;
        var orderNumber = _capturedOrder.OrderNumber;
        var totalAmount = _capturedOrder.TotalAmount;

        // === STEP 2: OrderConfirmedRevenue — create income GL entry ===
        await _revenueHandler.HandleAsync(
            orderId, tenantId, orderNumber, totalAmount, storeId: null, CancellationToken.None);

        _capturedIncome.Should().NotBeNull("Income record should be created");
        _capturedIncome!.OrderId.Should().Be(orderId, "Income should reference the order");
        _capturedIncome.Amount.Should().Be(totalAmount, "Income amount should match order total");
        _capturedIncome.IncomeType.Should().Be(IncomeType.Satis, "Income type should be Satis");
        _incomeRepo.Verify(r => r.AddAsync(It.IsAny<Income>()), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));

        // === STEP 3: CommissionChargedGL — commission expense GL entry ===
        var commissionRate = 0.12m;
        var commissionAmount = totalAmount * commissionRate;

        await _commissionHandler.HandleAsync(
            orderId, tenantId, PlatformType.Trendyol,
            commissionAmount, commissionRate, CancellationToken.None);

        // CommissionChargedGLHandler creates JournalEntry internally and saves via UoW
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(3));

        // === STEP 4: OrderShippedCost — shipping cost GL entry ===
        var shippingCost = 29.90m;

        await _shippedCostHandler.HandleAsync(
            orderId, tenantId, "TR1234567890",
            CargoProvider.YurticiKargo, shippingCost, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(4));
    }

    [Fact]
    public async Task FullChain_MultipleItems_TotalAmountFlowsCorrectly()
    {
        // Arrange — 3 products
        var product1 = CreateAndRegisterProduct("MULTI-01", 50, 100m);
        var product2 = CreateAndRegisterProduct("MULTI-02", 30, 200m);
        var product3 = CreateAndRegisterProduct("MULTI-03", 20, 350m);

        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Multi Customer", "multi@test.com", null,
            new List<PlaceOrderItem>
            {
                new(product1.Id, 3, 100m, 0.18m),
                new(product2.Id, 1, 200m, 0.18m),
                new(product3.Id, 2, 350m, 0.18m)
            });

        // Step 1: Place
        var result = await _placeOrderHandler.Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        _capturedOrder.Should().NotBeNull();

        var expectedSubTotal = (3 * 100m) + (1 * 200m) + (2 * 350m); // 1200
        _capturedOrder!.SubTotal.Should().Be(expectedSubTotal);
        _capturedOrder.OrderItems.Should().HaveCount(3);

        // Step 2: Revenue — total amount should match
        var tenantId = Guid.NewGuid();
        await _revenueHandler.HandleAsync(
            _capturedOrder.Id, tenantId, _capturedOrder.OrderNumber,
            _capturedOrder.TotalAmount, null, CancellationToken.None);

        _capturedIncome.Should().NotBeNull();
        _capturedIncome!.Amount.Should().Be(_capturedOrder.TotalAmount,
            "Revenue amount must equal order TotalAmount (including tax)");
    }

    [Fact]
    public async Task Chain_ZeroCommission_SkipsGLEntry_ButShippingStillRecorded()
    {
        // Arrange
        var product = CreateAndRegisterProduct("ZERO-COM", 10, 500m);

        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Direct Sale", "direct@test.com", null,
            new List<PlaceOrderItem> { new(product.Id, 1, 500m) });

        var result = await _placeOrderHandler.Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        var tenantId = Guid.NewGuid();
        var orderId = _capturedOrder!.Id;

        // Revenue
        await _revenueHandler.HandleAsync(
            orderId, tenantId, _capturedOrder.OrderNumber,
            _capturedOrder.TotalAmount, null, CancellationToken.None);

        var saveCountAfterRevenue = _uow.Invocations
            .Count(i => i.Method.Name == "SaveChangesAsync");

        // Zero commission — should skip
        await _commissionHandler.HandleAsync(
            orderId, tenantId, PlatformType.Trendyol, 0m, 0m, CancellationToken.None);

        var saveCountAfterCommission = _uow.Invocations
            .Count(i => i.Method.Name == "SaveChangesAsync");
        saveCountAfterCommission.Should().Be(saveCountAfterRevenue,
            "Zero commission should not trigger SaveChanges");

        // Shipping still recorded
        await _shippedCostHandler.HandleAsync(
            orderId, tenantId, "TR9999", CargoProvider.ArasKargo, 45m, CancellationToken.None);

        var saveCountAfterShipping = _uow.Invocations
            .Count(i => i.Method.Name == "SaveChangesAsync");
        saveCountAfterShipping.Should().Be(saveCountAfterRevenue + 1,
            "Shipping cost should trigger one SaveChanges");
    }

    [Fact]
    public async Task Chain_ZeroShippingCost_SkipsShippingGL()
    {
        var product = CreateAndRegisterProduct("FREE-SHIP", 20, 150m);

        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Free Shipping", "free@test.com", null,
            new List<PlaceOrderItem> { new(product.Id, 1, 150m) });

        var result = await _placeOrderHandler.Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        var tenantId = Guid.NewGuid();
        var orderId = _capturedOrder!.Id;

        // Revenue + Commission
        await _revenueHandler.HandleAsync(
            orderId, tenantId, _capturedOrder.OrderNumber,
            _capturedOrder.TotalAmount, null, CancellationToken.None);
        await _commissionHandler.HandleAsync(
            orderId, tenantId, PlatformType.Hepsiburada, 22.5m, 0.15m, CancellationToken.None);

        var saveCountBefore = _uow.Invocations
            .Count(i => i.Method.Name == "SaveChangesAsync");

        // Zero shipping — should skip
        await _shippedCostHandler.HandleAsync(
            orderId, tenantId, "TR0000", CargoProvider.SuratKargo, 0m, CancellationToken.None);

        var saveCountAfter = _uow.Invocations
            .Count(i => i.Method.Name == "SaveChangesAsync");
        saveCountAfter.Should().Be(saveCountBefore,
            "Zero shipping cost should not trigger SaveChanges");
    }

    [Fact]
    public async Task Chain_ProductNotFound_PlaceOrderFails_NoDownstreamHandlersCalled()
    {
        var missingProductId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingProductId)).ReturnsAsync((Product?)null);

        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Ghost Customer", "ghost@test.com", null,
            new List<PlaceOrderItem> { new(missingProductId, 1, 100m) });

        var result = await _placeOrderHandler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse("Missing product should fail order placement");
        result.ErrorMessage.Should().Contain(missingProductId.ToString());
        _capturedOrder.Should().BeNull("Order should not be persisted when product is missing");
        _capturedIncome.Should().BeNull("No income should be recorded for a failed order");

        // UoW should never be called
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Chain_StockDeducted_AfterPlaceOrder()
    {
        var product = CreateAndRegisterProduct("STK-DED", 50, 200m);

        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Stock Test", "stock@test.com", null,
            new List<PlaceOrderItem> { new(product.Id, 5, 200m) });

        await _placeOrderHandler.Handle(command, CancellationToken.None);

        product.Stock.Should().Be(45, "Stock should decrease by ordered quantity (50 - 5)");
    }

    [Fact]
    public async Task Chain_OrderNumber_FlowsThroughRevenueDescription()
    {
        var product = CreateAndRegisterProduct("DESC-FLO", 10, 100m);

        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Desc Customer", "desc@test.com", null,
            new List<PlaceOrderItem> { new(product.Id, 1, 100m) });

        await _placeOrderHandler.Handle(command, CancellationToken.None);
        _capturedOrder.Should().NotBeNull();
        _capturedOrder!.OrderNumber.Should().StartWith("ORD-");

        await _revenueHandler.HandleAsync(
            _capturedOrder.Id, Guid.NewGuid(), _capturedOrder.OrderNumber,
            _capturedOrder.TotalAmount, null, CancellationToken.None);

        _capturedIncome.Should().NotBeNull();
        _capturedIncome!.Description.Should().Contain(_capturedOrder.OrderNumber,
            "Revenue description should include the order number for traceability");
    }
}
