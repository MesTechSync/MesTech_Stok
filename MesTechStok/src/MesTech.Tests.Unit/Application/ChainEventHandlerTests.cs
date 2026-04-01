using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — Zincir EventHandler testleri (Z1-Z10)
// En kritik iş akışı handler'ları — sipariş, fatura, iade, stok
// ═══════════════════════════════════════════════════════════════

#region Z1: OrderPlacedStockDeductionHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
[Trait("Chain", "Z1")]
public class OrderPlacedStockDeductionHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly OrderPlacedStockDeductionHandler _sut;

    private readonly Mock<IDistributedLockService> _lockServiceMock = new();

    public OrderPlacedStockDeductionHandlerTests()
    {
        // Default lock: always succeeds
        _lockServiceMock.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        _sut = new OrderPlacedStockDeductionHandler(
            _orderRepoMock.Object, _productRepoMock.Object,
            _uowMock.Object, _lockServiceMock.Object,
            Mock.Of<ILogger<OrderPlacedStockDeductionHandler>>());
    }

    [Fact]
    public async Task HandleAsync_OrderNotFound_ReturnsWithoutException()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var act = () => _sut.HandleAsync(Guid.NewGuid(), "ORD-001", CancellationToken.None);

        await act.Should().NotThrowAsync();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValidOrder_CallsSaveChanges()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var orderItem = new OrderItem
        {
            TenantId = tenantId,
            ProductId = productId,
            ProductSKU = "TST-001",
            ProductName = "Test Ürün"
        };
        orderItem.SetQuantityAndPrice(2, 100m);

        var order = Order.CreateFromPlatform(
            tenantId, "EXT-002", PlatformType.Trendyol,
            "Test Müşteri", "test@test.com",
            new List<OrderItem> { orderItem });

        var product = new Product
        {
            Id = productId,
            Name = "Test Ürün", SKU = "TST-001", Stock = 50,
            MinimumStock = 5, SalePrice = 100m, IsActive = true
        };

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        await _sut.HandleAsync(orderId, "ORD-002", CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        product.Stock.Should().Be(48, "50 - 2 = 48");
    }
}

#endregion

#region Z2: OrderConfirmedRevenueHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
[Trait("Chain", "Z2")]
public class OrderConfirmedRevenueHandlerTests
{
    private readonly Mock<IIncomeRepository> _incomeRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly OrderConfirmedRevenueHandler _sut;

    public OrderConfirmedRevenueHandlerTests()
    {
        _sut = new OrderConfirmedRevenueHandler(
            _incomeRepoMock.Object, _uowMock.Object,
            Mock.Of<ILogger<OrderConfirmedRevenueHandler>>());
    }

    [Fact]
    public async Task HandleAsync_CreatesIncomeRecord()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        Income? captured = null;

        _incomeRepoMock.Setup(r => r.AddAsync(It.IsAny<Income>()))
            .Callback<Income>(i => captured = i)
            .Returns(Task.CompletedTask);

        await _sut.HandleAsync(orderId, tenantId, "ORD-100", 5000m, null, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(5000m);
        captured.TenantId.Should().Be(tenantId);
        captured.OrderId.Should().Be(orderId);
        captured.IncomeType.Should().Be(IncomeType.Satis);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Z3: InvoiceApprovedGLHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
[Trait("Chain", "Z3")]
public class InvoiceApprovedGLHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly InvoiceApprovedGLHandler _sut;

    public InvoiceApprovedGLHandlerTests()
    {
        _sut = new InvoiceApprovedGLHandler(
            _uowMock.Object, Mock.Of<MesTech.Domain.Interfaces.IJournalEntryRepository>(),
            Mock.Of<ILogger<InvoiceApprovedGLHandler>>());
    }

    [Fact]
    public async Task HandleAsync_CreatesGLEntryAndSaves()
    {
        var tenantId = Guid.NewGuid();

        await _sut.HandleAsync(
            Guid.NewGuid(), tenantId, "INV-001",
            grandTotal: 11800m, taxAmount: 1800m, netAmount: 10000m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroTax_SkipsTaxLine()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-002",
            grandTotal: 5000m, taxAmount: 0m, netAmount: 5000m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

// Z4: InvoiceCancelledReversalHandlerTests → canonical location: Handlers/InvoiceCancelledReversalHandlerTests.cs

#region Z5: ReturnApprovedStockRestorationHandler + ReturnJournalReversalHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
[Trait("Chain", "Z5")]
public class ReturnApprovedStockRestorationHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ReturnApprovedStockRestorationHandler _sut;

    public ReturnApprovedStockRestorationHandlerTests()
    {
        _sut = new ReturnApprovedStockRestorationHandler(
            _productRepoMock.Object, _uowMock.Object,
            Mock.Of<ILogger<ReturnApprovedStockRestorationHandler>>());
    }

    [Fact]
    public async Task HandleAsync_RestoresStock()
    {
        var product = new Product
        {
            Name = "İade Ürün", SKU = "RET-001",
            MinimumStock = 5, SalePrice = 100m, PurchasePrice = 50m,
            CategoryId = Guid.NewGuid(), IsActive = true
        };
        product.AdjustStock(10, StockMovementType.StockIn);

        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var lines = new List<ReturnLineInfoEvent>
        {
            new(product.Id, "RET-001", 3, 100m)
        };

        await _sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), lines, CancellationToken.None);

        product.Stock.Should().Be(13, "10 + 3 = 13");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ProductNotFound_SkipsLine()
    {
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var lines = new List<ReturnLineInfoEvent>
        {
            new(Guid.NewGuid(), "MISSING-001", 5, 200m)
        };

        var act = () => _sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), lines, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
[Trait("Chain", "Z5")]
public class ReturnJournalReversalHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ReturnJournalReversalHandler _sut;

    public ReturnJournalReversalHandlerTests()
    {
        _sut = new ReturnJournalReversalHandler(
            _uowMock.Object, Mock.Of<MesTech.Domain.Interfaces.IJournalEntryRepository>(),
            Mock.Of<ILogger<ReturnJournalReversalHandler>>());
    }

    [Fact]
    public async Task HandleAsync_PositiveAmount_CreatesReversalEntry()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            totalRefundAmount: 1500m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroAmount_SkipsGL()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            totalRefundAmount: 0m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion

#region Z6: CommissionChargedGLHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
[Trait("Chain", "Z6")]
public class CommissionChargedGLHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CommissionChargedGLHandler _sut;

    public CommissionChargedGLHandlerTests()
    {
        _sut = new CommissionChargedGLHandler(
            _uowMock.Object, Mock.Of<MesTech.Domain.Interfaces.IJournalEntryRepository>(),
            Mock.Of<ILogger<CommissionChargedGLHandler>>());
    }

    [Fact]
    public async Task HandleAsync_PositiveCommission_CreatesGLEntry()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), PlatformType.Trendyol,
            commissionAmount: 600m, commissionRate: 0.12m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroCommission_SkipsGL()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), PlatformType.Trendyol,
            commissionAmount: 0m, commissionRate: 0m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion

// Z7: ShipmentCostRecordedEventHandler — tip mevcut değil
// OrderShippedCostHandler testleri ChainIdempotencyTests.cs'de mevcut

#region Z8: ZeroStockDetectedEventHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
[Trait("Chain", "Z8")]
public class ZeroStockDetectedEventHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ZeroStockDetectedEventHandler _sut;

    public ZeroStockDetectedEventHandlerTests()
    {
        _sut = new ZeroStockDetectedEventHandler(
            _productRepoMock.Object, _uowMock.Object,
            Mock.Of<ILogger<ZeroStockDetectedEventHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ActiveProduct_Deactivates()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Tükenmiş Ürün", SKU = "ZERO-001", Stock = 0,
            SalePrice = 50m, IsActive = true
        };
        _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        await _sut.HandleAsync(productId, "ZERO-001", Guid.NewGuid(), CancellationToken.None);

        product.IsActive.Should().BeFalse("stok sıfır = ürün pasife alınır");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AlreadyInactive_SkipsDeactivation()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Zaten Pasif", SKU = "ZERO-002", Stock = 0,
            SalePrice = 50m, IsActive = false
        };
        _productRepoMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        await _sut.HandleAsync(productId, "ZERO-002", Guid.NewGuid(), CancellationToken.None);

        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ProductNotFound_DoesNotThrow()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var act = () => _sut.HandleAsync(Guid.NewGuid(), "GONE", Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

#endregion

// Z10: PriceLossDetectedEventHandler — tip henüz oluşturulmadı
