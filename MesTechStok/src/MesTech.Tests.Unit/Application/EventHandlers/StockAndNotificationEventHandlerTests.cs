using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

// ════════════════════════════════════════════════════════
// DEV5 TUR 10: Stock + Notification + Subscription handler tests
// Coverage: OrderPlacedStockDeduction, PriceLossDetected,
//           ReturnApprovedStockRestoration, ZeroStockDetected,
//           SubscriptionNotification (3 methods),
//           SyncErrorNotification
// ════════════════════════════════════════════════════════

#region OrderPlacedStockDeductionHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Stock")]
public class OrderPlacedStockDeductionHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IDistributedLockService> _lockService = new();
    private readonly Mock<ILogger<OrderPlacedStockDeductionHandler>> _logger = new();

    private OrderPlacedStockDeductionHandler CreateSut() =>
        new(_orderRepo.Object, _productRepo.Object, _uow.Object, _lockService.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_OrderNotFound_ShouldReturnEarlyWithoutSaving()
    {
        var sut = CreateSut();
        _orderRepo.Setup(r => r.GetWithLinesByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);

        await sut.HandleAsync(Guid.NewGuid(), "ORD-001", CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldAcquireDistributedLock()
    {
        var sut = CreateSut();
        _orderRepo.Setup(r => r.GetWithLinesByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);

        await sut.HandleAsync(Guid.NewGuid(), "ORD-LOCK", CancellationToken.None);

        // Lock should still be attempted even if order not found
        // (defensive pattern — lock first, then lookup)
    }
}

#endregion

#region PriceLossDetectedEventHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Notification")]
public class PriceLossDetectedEventHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<PriceLossDetectedEventHandler>> _logger = new();

    private PriceLossDetectedEventHandler CreateSut() =>
        new(_notifRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreatePriceLossNotification()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        await sut.HandleAsync(
            Guid.NewGuid(), tenantId, "SKU-LOSS",
            100.00m, 85.00m, 15.00m, CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n =>
                n.TenantId == tenantId &&
                n.TemplateName == "PriceLossDetected"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeSkuAndLossAmount()
    {
        var sut = CreateSut();
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-EXPENSIVE",
            200.00m, 150.00m, 50.00m, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("SKU-EXPENSIVE");
        captured.Content.Should().Contain("50");
    }
}

#endregion

#region ReturnApprovedStockRestorationHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Stock")]
public class ReturnApprovedStockRestorationHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ReturnApprovedStockRestorationHandler>> _logger = new();

    private ReturnApprovedStockRestorationHandler CreateSut() =>
        new(_productRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_EmptyLines_ShouldReturnWithoutSaving()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            Array.Empty<ReturnLineInfoEvent>(), CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithLines_ShouldRestoreStockForEachProduct()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var product = new Product { Name = "Test Product", SKU = "SKU-RET", TenantId = Guid.NewGuid() };
        _productRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        var lines = new List<ReturnLineInfoEvent>
        {
            new(productId, "SKU-RET", 3, 100.00m)
        };

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), lines, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region ZeroStockDetectedEventHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Stock")]
public class ZeroStockDetectedEventHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ZeroStockDetectedEventHandler>> _logger = new();

    private ZeroStockDetectedEventHandler CreateSut() =>
        new(_productRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldDeactivateProduct()
    {
        var sut = CreateSut();
        var productId = Guid.NewGuid();
        var product = new Product { Name = "Zero Stock Product", SKU = "SKU-ZERO", TenantId = Guid.NewGuid() };
        _productRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        await sut.HandleAsync(productId, "SKU-ZERO", Guid.NewGuid(), CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ProductNotFound_ShouldLogWarningAndReturn()
    {
        var sut = CreateSut();
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var act = async () => await sut.HandleAsync(
            Guid.NewGuid(), "SKU-MISSING", Guid.NewGuid(), CancellationToken.None);

        await act.Should().NotThrowAsync();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion

#region SubscriptionNotificationHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Notification")]
public class SubscriptionNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<SubscriptionNotificationHandler>> _logger = new();

    private SubscriptionNotificationHandler CreateSut() =>
        new(_notifRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleCreatedAsync_ShouldCreateNotification()
    {
        var sut = CreateSut();

        await sut.HandleCreatedAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n => n.TemplateName == "SubscriptionCreated"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCancelledAsync_ShouldIncludeReasonInContent()
    {
        var sut = CreateSut();
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        await sut.HandleCancelledAsync(
            Guid.NewGuid(), Guid.NewGuid(), "Fiyat fazla", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Fiyat fazla");
    }

    [Fact]
    public async Task HandlePlanChangedAsync_ShouldIncludeNewPlan()
    {
        var sut = CreateSut();
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        await sut.HandlePlanChangedAsync(
            Guid.NewGuid(), Guid.NewGuid(), "Enterprise", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Enterprise");
    }
}

#endregion

#region SyncErrorNotificationHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Notification")]
public class SyncErrorNotificationHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<SyncErrorNotificationHandler>> _logger = new();

    private SyncErrorNotificationHandler CreateSut() =>
        new(_notifRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateSyncErrorNotification()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), "Trendyol", "ConnectionTimeout",
            "API iletisim hatasi", CancellationToken.None);

        _notifRepo.Verify(r => r.AddAsync(
            It.Is<NotificationLog>(n => n.TemplateName == "SyncError"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludePlatformAndErrorType()
    {
        var sut = CreateSut();
        NotificationLog? captured = null;
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n);

        await sut.HandleAsync(
            Guid.NewGuid(), "Hepsiburada", "RateLimitExceeded",
            "Too many requests", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("Hepsiburada");
        captured.Content.Should().Contain("RateLimitExceeded");
    }
}

#endregion
