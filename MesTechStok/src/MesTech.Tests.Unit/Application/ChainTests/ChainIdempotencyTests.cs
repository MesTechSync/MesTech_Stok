using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using IJournalEntryRepository = MesTech.Domain.Interfaces.IJournalEntryRepository;

namespace MesTech.Tests.Unit.Application.ChainTests;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — Zincir handler idempotency + edge case testleri
// Z1 lock, Z3/Z4/Z5/Z7 duplicate guard, Z8 already-inactive
// ═══════════════════════════════════════════════════════════════

#region Z1: OrderPlacedStockDeductionHandler — distributed lock

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
[Trait("Chain", "Z1")]
public class Z1_OrderPlaced_LockTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDistributedLockService> _lockServiceMock = new();
    private readonly OrderPlacedStockDeductionHandler _sut;

    public Z1_OrderPlaced_LockTests()
    {
        _lockServiceMock.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        _sut = new OrderPlacedStockDeductionHandler(
            _orderRepoMock.Object, _productRepoMock.Object,
            _uowMock.Object, _lockServiceMock.Object,
            Mock.Of<ILogger<OrderPlacedStockDeductionHandler>>());
    }

    [Fact]
    public async Task HandleAsync_LockFailed_ReturnsWithoutDeduction()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var orderItem = new OrderItem
        {
            TenantId = tenantId, ProductId = productId,
            ProductSKU = "LOCK-FAIL", ProductName = "Lock Fail Ürün"
        };
        orderItem.SetQuantityAndPrice(5, 50m);

        var order = Order.CreateFromPlatform(
            tenantId, "EXT-LOCK", PlatformType.Trendyol,
            "Test", "t@t.com", new List<OrderItem> { orderItem });

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _lockServiceMock.Setup(l => l.AcquireLockAsync(
                It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        await _sut.HandleAsync(orderId, "ORD-LOCK", CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_MultipleItems_DeductsAllStocks()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var prod1Id = Guid.NewGuid();
        var prod2Id = Guid.NewGuid();

        var item1 = new OrderItem { TenantId = tenantId, ProductId = prod1Id, ProductSKU = "SKU-A", ProductName = "A" };
        item1.SetQuantityAndPrice(3, 100m);
        var item2 = new OrderItem { TenantId = tenantId, ProductId = prod2Id, ProductSKU = "SKU-B", ProductName = "B" };
        item2.SetQuantityAndPrice(2, 200m);

        var order = Order.CreateFromPlatform(
            tenantId, "EXT-MULTI", PlatformType.Hepsiburada,
            "Test", "t@t.com", new List<OrderItem> { item1, item2 });

        var product1 = new Product { Name = "A", SKU = "SKU-A", Stock = 100, SalePrice = 100m, IsActive = true };
        var product2 = new Product { Name = "B", SKU = "SKU-B", Stock = 50, SalePrice = 200m, IsActive = true };

        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1, product2 });

        await _sut.HandleAsync(orderId, "ORD-MULTI", CancellationToken.None);

        product1.Stock.Should().Be(97, "100 - 3");
        product2.Stock.Should().Be(48, "50 - 2");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Z3: InvoiceApprovedGLHandler — idempotency guard

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
[Trait("Chain", "Z3")]
public class Z3_InvoiceGL_IdempotencyTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IJournalEntryRepository> _journalRepoMock = new();
    private readonly InvoiceApprovedGLHandler _sut;

    public Z3_InvoiceGL_IdempotencyTests()
    {
        _sut = new InvoiceApprovedGLHandler(
            _uowMock.Object, _journalRepoMock.Object,
            Mock.Of<ILogger<InvoiceApprovedGLHandler>>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateInvoice_SkipsGL()
    {
        var tenantId = Guid.NewGuid();
        _journalRepoMock.Setup(r => r.ExistsByReferenceAsync(tenantId, "INV-DUP", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.HandleAsync(
            Guid.NewGuid(), tenantId, "INV-DUP",
            grandTotal: 11800m, taxAmount: 1800m, netAmount: 10000m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ZeroGrandTotal_SkipsGL()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-ZERO",
            grandTotal: 0m, taxAmount: 0m, netAmount: 0m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NewInvoice_CreatesGLAndSaves()
    {
        var tenantId = Guid.NewGuid();
        _journalRepoMock.Setup(r => r.ExistsByReferenceAsync(tenantId, "INV-NEW", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _sut.HandleAsync(
            Guid.NewGuid(), tenantId, "INV-NEW",
            grandTotal: 11800m, taxAmount: 1800m, netAmount: 10000m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Z5: ReturnJournalReversalHandler — idempotency guard

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
[Trait("Chain", "Z5")]
public class Z5_ReturnReversal_IdempotencyTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IJournalEntryRepository> _journalRepoMock = new();
    private readonly ReturnJournalReversalHandler _sut;

    public Z5_ReturnReversal_IdempotencyTests()
    {
        _sut = new ReturnJournalReversalHandler(
            _uowMock.Object, _journalRepoMock.Object,
            Mock.Of<ILogger<ReturnJournalReversalHandler>>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateRef_SkipsGL()
    {
        _journalRepoMock.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            totalRefundAmount: 2000m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ZeroAmount_SkipsGL()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            totalRefundAmount: 0m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PositiveAmount_CreatesReversalAndSaves()
    {
        _journalRepoMock.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            totalRefundAmount: 1500m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Z7: OrderShippedCostHandler — idempotency guard

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
[Trait("Chain", "Z7")]
public class Z7_ShippedCost_IdempotencyTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IJournalEntryRepository> _journalRepoMock = new();
    private readonly OrderShippedCostHandler _sut;

    public Z7_ShippedCost_IdempotencyTests()
    {
        _sut = new OrderShippedCostHandler(
            _uowMock.Object, _journalRepoMock.Object,
            Mock.Of<ILogger<OrderShippedCostHandler>>());
    }

    [Fact]
    public async Task HandleAsync_PositiveCost_CreatesGLAndSaves()
    {
        var tenantId = Guid.NewGuid();
        _journalRepoMock.Setup(r => r.ExistsByReferenceAsync(tenantId, "TR-NEW", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _sut.HandleAsync(
            Guid.NewGuid(), tenantId, "TR-NEW",
            CargoProvider.YurticiKargo, 45.50m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroCost_SkipsGL()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "TR-ZERO",
            CargoProvider.ArasKargo, 0m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DuplicateTracking_SkipsGL()
    {
        var tenantId = Guid.NewGuid();
        _journalRepoMock.Setup(r => r.ExistsByReferenceAsync(tenantId, "TR-DUP", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.HandleAsync(
            Guid.NewGuid(), tenantId, "TR-DUP",
            CargoProvider.SuratKargo, 30m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion
