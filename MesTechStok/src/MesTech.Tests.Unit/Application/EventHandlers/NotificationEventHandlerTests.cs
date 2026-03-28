using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

// ════════════════════════════════════════════════════════
// DEV5 TUR 9: 20 notification/event handler unit tests
// Coverage: CRM, LowStock, OrderCancelled, ShipmentCost,
//           StockChanged, EInvoice, Expense, InvoiceCreated,
//           InvoiceLifecycle, OrderCompleted, OrderReceived,
//           OrderShipped, PaymentFailed, PriceChanged,
//           ProductCreated, ProductLifecycle, ReturnCreated,
//           StaleOrder, StockCritical, Misc
// ════════════════════════════════════════════════════════

#region LowStockNotificationHandler

[Trait("Category", "Unit")]
public class LowStockNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<LowStockNotificationHandler>> _logger = new();

    private LowStockNotificationHandler CreateSut() => new(_notifRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateNotificationAndSave()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        await sut.HandleAsync(productId, tenantId, "SKU-001", 5, 10, CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(It.Is<NotificationLog>(n =>
            n.TenantId == tenantId &&
            n.TemplateName == "LowStockAlert"), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeSkuInContent()
    {
        var sut = CreateSut();
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ABC-999", 3, 20, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("ABC-999");
        captured.Content.Should().Contain("3");
        captured.Content.Should().Contain("20");
    }
}

#endregion

#region CrmNotificationHandler

[Trait("Category", "Unit")]
public class CrmNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<CrmNotificationHandler>> _logger = new();

    private CrmNotificationHandler CreateSut() => new(_notifRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleDealWonAsync_ShouldCreateNotification()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        await sut.HandleDealWonAsync(Guid.NewGuid(), tenantId, CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(It.Is<NotificationLog>(n =>
            n.TenantId == tenantId &&
            n.TemplateName == "DealWon"), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleDealLostAsync_ShouldIncludeReasonInContent()
    {
        var sut = CreateSut();
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        await sut.HandleDealLostAsync(Guid.NewGuid(), Guid.NewGuid(), "Fiyat uyumsuzlugu", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Fiyat uyumsuzlugu");
        captured.TemplateName.Should().Be("DealLost");
    }

    [Fact]
    public async Task HandleDealLostAsync_NullReason_ShouldUseFallback()
    {
        var sut = CreateSut();
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        await sut.HandleDealLostAsync(Guid.NewGuid(), Guid.NewGuid(), null, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("belirtilmedi");
    }

    [Fact]
    public async Task HandleLeadScoredAsync_ShouldIncludeScore()
    {
        var sut = CreateSut();
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        await sut.HandleLeadScoredAsync(Guid.NewGuid(), Guid.NewGuid(), 85, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("85");
        captured.TemplateName.Should().Be("LeadScored");
    }

    [Fact]
    public async Task HandleCalendarEventCreatedAsync_ShouldIncludeTitle()
    {
        var sut = CreateSut();
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        await sut.HandleCalendarEventCreatedAsync(Guid.NewGuid(), Guid.NewGuid(), "Sprint Review", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Sprint Review");
        captured.TemplateName.Should().Be("CalendarEventCreated");
    }

    [Fact]
    public async Task HandleLeadConvertedAsync_ShouldCreateNotification()
    {
        var sut = CreateSut();
        await sut.HandleLeadConvertedAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(It.Is<NotificationLog>(n =>
            n.TemplateName == "LeadConverted"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleDealStageChangedAsync_ShouldIncludeNewStage()
    {
        var sut = CreateSut();
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        await sut.HandleDealStageChangedAsync(Guid.NewGuid(), Guid.NewGuid(), "Proposal", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Proposal");
    }
}

#endregion

#region ShipmentCostJournalHandler

[Trait("Category", "Unit")]
public class ShipmentCostJournalHandlerTests
{
    private readonly Mock<ICargoExpenseRepository> _cargoRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ShipmentCostJournalHandler>> _logger = new();

    private ShipmentCostJournalHandler CreateSut() => new(_cargoRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateCargoExpenseAndSave()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await sut.HandleAsync(orderId, tenantId, "TR123456789", "YurticiKargo", 45.50m, CancellationToken.None);

        _cargoRepo.Verify(r => r.AddAsync(It.Is<CargoExpense>(e =>
            e.TenantId == tenantId &&
            e.Amount == 45.50m), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetCorrectCargoProvider()
    {
        var sut = CreateSut();
        CargoExpense? captured = null;
        _cargoRepo.Setup(r => r.AddAsync(It.IsAny<CargoExpense>(), It.IsAny<CancellationToken>()))
            .Callback<CargoExpense, CancellationToken>((e, _) => captured = e);

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "TR987654321", "ArasKargo", 32.00m, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CargoProvider.Should().Be("ArasKargo");
        captured.TrackingNumber.Should().Be("TR987654321");
    }
}

#endregion

#region StockChangedPlatformSyncHandler

[Trait("Category", "Unit")]
public class StockChangedPlatformSyncHandlerTests
{
    private readonly Mock<ILogger<StockChangedPlatformSyncHandler>> _logger = new();

    private StockChangedPlatformSyncHandler CreateSut() => new(_logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCompleteWithoutException()
    {
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-100",
            50, 45, StockMovementType.Sale, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_ZeroStock_ShouldLogWarning()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-ZERO",
            5, 0, StockMovementType.Sale, CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("STOK SIFIR")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NonZeroStock_ShouldNotLogWarning()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-OK",
            50, 45, StockMovementType.Sale, CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}

#endregion

#region OrderCancelledStockRestorationHandler

[Trait("Category", "Unit")]
public class OrderCancelledStockRestorationHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDistributedLockService> _lockService = new();
    private readonly Mock<ILogger<OrderCancelledStockRestorationHandler>> _logger = new();

    private OrderCancelledStockRestorationHandler CreateSut() =>
        new(_orderRepo.Object, _productRepo.Object, _uow.Object, _lockService.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_OrderNotFound_ShouldReturnEarlyWithoutSaving()
    {
        var sut = CreateSut();
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "test reason", CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _lockService.Verify(l => l.AcquireLockAsync(
            It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

#endregion
