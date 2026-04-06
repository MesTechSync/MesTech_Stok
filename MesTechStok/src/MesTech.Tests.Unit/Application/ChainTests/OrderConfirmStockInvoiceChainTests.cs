using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using IOrderRepository = MesTech.Domain.Interfaces.IOrderRepository;
using IProductRepository = MesTech.Domain.Interfaces.IProductRepository;
using IInvoiceRepository = MesTech.Domain.Interfaces.IInvoiceRepository;
using InvoiceEntity = MesTech.Domain.Entities.Invoice;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.ChainTests;

/// <summary>
/// TEST 2/4 — OrderConfirmed → StockDeduction → Invoice zincir testi.
/// Z1: Order.Confirm → StockDeductionHandler → stok düşer.
/// Z2b: OrderCompleted → InvoiceHandler → Invoice oluşur.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "OrderChain")]
public class OrderConfirmStockInvoiceChainTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDistributedLockService> _lock = new();

    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public OrderConfirmStockInvoiceChainTests()
    {
        _lock.Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task Z1_OrderPlaced_StockDeducted()
    {
        var product = new Product
        {
            SKU = "ORD-P1", Name = "Test Urun", MinimumStock = 5,
            CategoryId = Guid.NewGuid(), TenantId = TenantId
        };
        product.SyncStock(100, "seed");

        var orderId = Guid.NewGuid();
        var order = new Order
        {
            OrderNumber = "ORD-CHAIN-Z1", TenantId = TenantId, CustomerId = Guid.NewGuid()
        };
        order.AddItem(new OrderItem
        {
            ProductId = product.Id, ProductSKU = "ORD-P1", ProductName = "Test Urun",
            Quantity = 5, UnitPrice = 100m, TotalPrice = 500m, TenantId = TenantId
        });

        _orderRepo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var handler = new OrderPlacedStockDeductionHandler(
            _orderRepo.Object, _productRepo.Object, _uow.Object, _lock.Object,
            Mock.Of<ILogger<OrderPlacedStockDeductionHandler>>());

        await handler.HandleAsync(orderId, "ORD-CHAIN-Z1", CancellationToken.None);

        product.Stock.Should().Be(95, "100 - 5 = 95");
    }

    [Fact]
    public async Task Z2b_OrderCompleted_InvoiceCreated()
    {
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-INV-001",
            TenantId = TenantId,
            CustomerId = Guid.NewGuid(),
            SourcePlatform = PlatformType.Trendyol
        };
        order.SetFinancials(500m, 90m, 590m);

        _orderRepo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _invoiceRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((InvoiceEntity?)null);

        InvoiceEntity? capturedInvoice = null;
        _invoiceRepo.Setup(r => r.AddAsync(It.IsAny<InvoiceEntity>(), It.IsAny<CancellationToken>()))
            .Callback<InvoiceEntity, CancellationToken>((inv, _) => capturedInvoice = inv)
            .Returns(Task.CompletedTask);

        var handler = new OrderCompletedInvoiceHandler(
            _orderRepo.Object, _invoiceRepo.Object, _uow.Object,
            Mock.Of<ILogger<OrderCompletedInvoiceHandler>>());

        await handler.HandleAsync(orderId, TenantId, "ORD-INV-001", 590m, CancellationToken.None);

        capturedInvoice.Should().NotBeNull("invoice should be created for completed order");
        capturedInvoice!.OrderId.Should().Be(orderId);
        capturedInvoice.GrandTotal.Should().Be(590m);
    }

    [Fact]
    public async Task Z2b_DuplicateOrder_ShouldNotCreateSecondInvoice()
    {
        var orderId = Guid.NewGuid();
        var existingInvoice = InvoiceEntity.CreateForOrder(
            new Order { Id = orderId, OrderNumber = "ORD-DUP", TenantId = TenantId, CustomerId = Guid.NewGuid() },
            InvoiceType.EArsiv, "MES-20260310-00001");

        _orderRepo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order { Id = orderId, TenantId = TenantId, OrderNumber = "ORD-DUP", CustomerId = Guid.NewGuid() });
        _invoiceRepo.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInvoice);

        var handler = new OrderCompletedInvoiceHandler(
            _orderRepo.Object, _invoiceRepo.Object, _uow.Object,
            Mock.Of<ILogger<OrderCompletedInvoiceHandler>>());

        await handler.HandleAsync(orderId, TenantId, "ORD-DUP", 590m, CancellationToken.None);

        _invoiceRepo.Verify(r => r.AddAsync(It.IsAny<InvoiceEntity>(), It.IsAny<CancellationToken>()), Times.Never,
            "duplicate invoice should not be created");
    }

    [Fact]
    public async Task Z1_MultipleItems_AllStocksDeducted()
    {
        var p1 = new Product { SKU = "P1", Name = "Urun 1", MinimumStock = 5, CategoryId = Guid.NewGuid(), TenantId = TenantId };
        p1.SyncStock(50, "seed");
        var p2 = new Product { SKU = "P2", Name = "Urun 2", MinimumStock = 5, CategoryId = Guid.NewGuid(), TenantId = TenantId };
        p2.SyncStock(30, "seed");

        var orderId = Guid.NewGuid();
        var order = new Order { OrderNumber = "ORD-MULTI", TenantId = TenantId, CustomerId = Guid.NewGuid() };
        order.AddItem(new OrderItem { ProductId = p1.Id, ProductSKU = "P1", ProductName = "U1", Quantity = 10, UnitPrice = 50m, TotalPrice = 500m, TenantId = TenantId });
        order.AddItem(new OrderItem { ProductId = p2.Id, ProductSKU = "P2", ProductName = "U2", Quantity = 5, UnitPrice = 100m, TotalPrice = 500m, TenantId = TenantId });

        _orderRepo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { p1, p2 });

        var handler = new OrderPlacedStockDeductionHandler(
            _orderRepo.Object, _productRepo.Object, _uow.Object, _lock.Object,
            Mock.Of<ILogger<OrderPlacedStockDeductionHandler>>());

        await handler.HandleAsync(orderId, "ORD-MULTI", CancellationToken.None);

        p1.Stock.Should().Be(40, "50 - 10");
        p2.Stock.Should().Be(25, "30 - 5");
    }

    [Fact]
    public async Task Z1_OrderNotFound_ShouldNotThrow()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = new OrderPlacedStockDeductionHandler(
            _orderRepo.Object, _productRepo.Object, _uow.Object, _lock.Object,
            Mock.Of<ILogger<OrderPlacedStockDeductionHandler>>());

        var act = () => handler.HandleAsync(Guid.NewGuid(), "ORD-NONE", CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
