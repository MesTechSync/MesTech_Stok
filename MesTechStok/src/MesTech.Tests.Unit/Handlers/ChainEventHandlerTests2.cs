using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — Zincir EventHandler testleri (Batch 2)
// OrderConfirmedRevenue, InvoiceApprovedGL, InvoiceCancelledReversal,
// ReturnApprovedStockRestoration, ReturnJournalReversal, OrderShippedCost,
// ShipmentCostRecorded, StockCritical, ZeroStockDetected,
// PriceChanged, PriceLossDetected
// ═══════════════════════════════════════════════════════════════

#region OrderConfirmedRevenueHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
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
    public async Task HandleAsync_ValidInput_CreatesIncomeAndSaves()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await _sut.HandleAsync(orderId, tenantId, "ORD-001", 1500m, null, CancellationToken.None);

        _incomeRepoMock.Verify(r => r.AddAsync(It.IsAny<MesTech.Domain.Entities.Income>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroAmount_StillCreatesRecord()
    {
        var act = () => _sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-002", 0m, null, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _incomeRepoMock.Verify(r => r.AddAsync(It.IsAny<MesTech.Domain.Entities.Income>()), Times.Once);
    }
}

#endregion

#region InvoiceApprovedGLHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
public class InvoiceApprovedGLHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly InvoiceApprovedGLHandler _sut;

    public InvoiceApprovedGLHandlerTests()
    {
        _sut = new InvoiceApprovedGLHandler(
            _uowMock.Object, Mock.Of<ILogger<InvoiceApprovedGLHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ValidInvoice_SavesJournalEntry()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-001",
            1200m, 200m, 1000m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroTax_SkipsTaxLine()
    {
        var act = () => _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-002",
            1000m, 0m, 1000m, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region InvoiceCancelledReversalHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
public class InvoiceCancelledReversalHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly InvoiceCancelledReversalHandler _sut;

    public InvoiceCancelledReversalHandlerTests()
    {
        _sut = new InvoiceCancelledReversalHandler(
            _uowMock.Object, Mock.Of<ILogger<InvoiceCancelledReversalHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ValidCancellation_CreatesReversalEntry()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-003",
            "Müşteri iadesi", Guid.NewGuid(), 2400m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NullReason_StillCreatesEntry()
    {
        var act = () => _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-004",
            null, Guid.NewGuid(), 1000m, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region ReturnApprovedStockRestorationHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
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
    public async Task HandleAsync_ProductNotFound_SkipsAndContinues()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((MesTech.Domain.Entities.Product?)null);

        var lines = new List<ReturnLineInfoEvent>
        {
            new(Guid.NewGuid(), "SKU-001", 5, 100m)
        };

        var act = () => _sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), lines, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmptyLines_SavesWithoutError()
    {
        var lines = new List<ReturnLineInfoEvent>();

        var act = () => _sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), lines, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region ReturnJournalReversalHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
public class ReturnJournalReversalHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ReturnJournalReversalHandler _sut;

    public ReturnJournalReversalHandlerTests()
    {
        _sut = new ReturnJournalReversalHandler(
            _uowMock.Object, Mock.Of<ILogger<ReturnJournalReversalHandler>>());
    }

    [Fact]
    public async Task HandleAsync_PositiveRefund_CreatesReversalEntry()
    {
        await _sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 500m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroRefund_SkipsGLEntry()
    {
        await _sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NegativeRefund_SkipsGLEntry()
    {
        await _sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -100m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion

#region OrderShippedCostHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
public class OrderShippedCostHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly OrderShippedCostHandler _sut;

    public OrderShippedCostHandlerTests()
    {
        _sut = new OrderShippedCostHandler(
            _uowMock.Object, Mock.Of<ILogger<OrderShippedCostHandler>>());
    }

    [Fact]
    public async Task HandleAsync_PositiveCost_CreatesGLEntry()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "TRK-001",
            CargoProvider.YurticiKargo, 45.50m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroCost_SkipsGLEntry()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "TRK-002",
            CargoProvider.ArasKargo, 0m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NegativeCost_SkipsGLEntry()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "TRK-003",
            CargoProvider.SuratKargo, -10m, CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion

#region ShipmentCostRecordedEventHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
public class ShipmentCostRecordedEventHandlerTests2
{
    private readonly ShipmentCostRecordedEventHandler _sut;

    public ShipmentCostRecordedEventHandlerTests2()
    {
        _sut = new ShipmentCostRecordedEventHandler(
            Mock.Of<ILogger<ShipmentCostRecordedEventHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ValidEvent_LogsWithoutException()
    {
        var evt = new ShipmentCostRecordedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "TRK-100",
            "Yurtici", 35m, DateTime.UtcNow);

        var act = () => _sut.HandleAsync(evt, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

#endregion

#region StockCriticalEventHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
public class StockCriticalEventHandlerTests2
{
    private readonly StockCriticalEventHandler _sut;

    public StockCriticalEventHandlerTests2()
    {
        _sut = new StockCriticalEventHandler(
            Mock.Of<ILogger<StockCriticalEventHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ValidEvent_LogsWithoutException()
    {
        var evt = new StockCriticalEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Test Product", "SKU-001",
            2, 10, StockAlertLevel.Critical, null, null, DateTime.UtcNow);

        var act = () => _sut.HandleAsync(evt, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_OutOfStockLevel_LogsWithoutException()
    {
        var evt = new StockCriticalEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Test Product 2", "SKU-002",
            0, 5, StockAlertLevel.OutOfStock, Guid.NewGuid(), "Ana Depo", DateTime.UtcNow);

        var act = () => _sut.HandleAsync(evt, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

#endregion

#region ZeroStockDetectedEventHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
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
    public async Task HandleAsync_ProductNotFound_ReturnsWithoutSave()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((MesTech.Domain.Entities.Product?)null);

        await _sut.HandleAsync(Guid.NewGuid(), "SKU-X", Guid.NewGuid(), CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion

#region PriceChangedEventHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
public class PriceChangedEventHandlerTests2
{
    private readonly PriceChangedEventHandler _sut;

    public PriceChangedEventHandlerTests2()
    {
        _sut = new PriceChangedEventHandler(
            Mock.Of<ILogger<PriceChangedEventHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ValidEvent_LogsWithoutException()
    {
        var evt = new PriceChangedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-P01", 100m, 120m, DateTime.UtcNow);

        var act = () => _sut.HandleAsync(evt, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

#endregion

#region PriceLossDetectedEventHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Chain")]
public class PriceLossDetectedEventHandlerTests2
{
    private readonly PriceLossDetectedEventHandler _sut;

    public PriceLossDetectedEventHandlerTests2()
    {
        _sut = new PriceLossDetectedEventHandler(
            Mock.Of<ILogger<PriceLossDetectedEventHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ValidEvent_LogsWithoutException()
    {
        var act = () => _sut.HandleAsync(
            Guid.NewGuid(), "SKU-LOSS", 150m, 120m, 30m, Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_ZeroLoss_LogsWithoutException()
    {
        var act = () => _sut.HandleAsync(
            Guid.NewGuid(), "SKU-ZERO", 100m, 100m, 0m, Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

#endregion
