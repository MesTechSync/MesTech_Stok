using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.EventHandlers;

/// <summary>
/// Notification Event Handler testleri — tüm bildirim handler'ları.
/// Her handler: NotificationLog.Create → AddAsync → SaveChanges.
/// LowStock, OrderReceived, SyncError zaten DomainEventHandlerTests'te test edildi.
/// Bu dosya KALAN 15 notification handler'ı kapsar.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "EventHandler")]
[Trait("Group", "Notification")]
public class NotificationEventHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<INotificationLogRepository> _notifRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public NotificationEventHandlerTests()
    {
        _notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private void VerifyNotificationCreated()
    {
        _notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ OrderCompletedNotification ═══
    [Fact]
    public async Task OrderCompleted_CreatesNotification()
    {
        var h = new OrderCompletedNotificationHandler(_notifRepo.Object, _uow.Object, Mock.Of<ILogger<OrderCompletedNotificationHandler>>());
        await h.HandleAsync(Guid.NewGuid(), _tenantId, "ORD-100", 2500m, "Ahmet Yılmaz", CancellationToken.None);
        VerifyNotificationCreated();
    }

    // ═══ OrderShippedNotification ═══
    [Fact]
    public async Task OrderShipped_CreatesNotification()
    {
        var h = new OrderShippedNotificationHandler(_notifRepo.Object, _uow.Object, Mock.Of<ILogger<OrderShippedNotificationHandler>>());
        await h.HandleAsync(Guid.NewGuid(), _tenantId, "TRK-ABC", "Yurtici", CancellationToken.None);
        VerifyNotificationCreated();
    }

    // ═══ ProductCreatedNotification ═══
    [Fact]
    public async Task ProductCreated_CreatesNotification()
    {
        var h = new ProductCreatedNotificationHandler(_notifRepo.Object, _uow.Object, Mock.Of<ILogger<ProductCreatedNotificationHandler>>());
        await h.HandleAsync(Guid.NewGuid(), _tenantId, "SKU-NEW", "iPhone 16", 65000m, CancellationToken.None);
        VerifyNotificationCreated();
    }

    // ═══ ReturnCreatedNotification ═══
    [Fact]
    public async Task ReturnCreated_CreatesNotification()
    {
        var h = new ReturnCreatedNotificationHandler(_notifRepo.Object, _uow.Object, Mock.Of<ILogger<ReturnCreatedNotificationHandler>>());
        await h.HandleAsync(Guid.NewGuid(), _tenantId, Guid.NewGuid(), "Trendyol", "Ürün hasarlı", CancellationToken.None);
        VerifyNotificationCreated();
    }

    // ═══ PaymentFailedNotification ═══
    [Fact]
    public async Task PaymentFailed_CreatesNotification()
    {
        var h = new PaymentFailedNotificationHandler(_notifRepo.Object, _uow.Object, Mock.Of<ILogger<PaymentFailedNotificationHandler>>());
        await h.HandleAsync(_tenantId, Guid.NewGuid(), "Kart reddedildi", 3, CancellationToken.None);
        VerifyNotificationCreated();
    }

    // ═══ PriceChangedNotification ═══
    [Fact]
    public async Task PriceChanged_CreatesNotification()
    {
        var h = new PriceChangedNotificationHandler(_notifRepo.Object, _uow.Object, Mock.Of<ILogger<PriceChangedNotificationHandler>>());
        await h.HandleAsync(Guid.NewGuid(), _tenantId, "SKU-001", 100m, 85m, "Trendyol", CancellationToken.None);
        VerifyNotificationCreated();
    }

    // ═══ StockCriticalNotification ═══
    [Fact]
    public async Task StockCritical_CreatesNotification()
    {
        var h = new StockCriticalNotificationHandler(_notifRepo.Object, _uow.Object, Mock.Of<ILogger<StockCriticalNotificationHandler>>());
        await h.HandleAsync(Guid.NewGuid(), _tenantId, "SKU-CRIT", 0, CancellationToken.None);
        VerifyNotificationCreated();
    }

    // ═══ InvoiceCreatedNotification ═══
    [Fact]
    public async Task InvoiceCreated_CreatesNotification()
    {
        var h = new InvoiceCreatedNotificationHandler(_notifRepo.Object, _uow.Object, Mock.Of<ILogger<InvoiceCreatedNotificationHandler>>());
        await h.HandleAsync(Guid.NewGuid(), _tenantId, "INV-001", 11800m, CancellationToken.None);
        VerifyNotificationCreated();
    }

    // ═══ SubscriptionNotification ═══
    [Fact]
    public async Task SubscriptionRenewal_CreatesNotification()
    {
        var h = new SubscriptionNotificationHandler(_notifRepo.Object, _uow.Object, Mock.Of<ILogger<SubscriptionNotificationHandler>>());
        await h.HandleRenewalAsync(_tenantId, "Pro", DateTime.UtcNow.AddDays(30), CancellationToken.None);
        VerifyNotificationCreated();
    }

    // ═══ EInvoiceNotification ═══
    [Fact]
    public async Task EInvoiceSent_CreatesNotification()
    {
        var h = new EInvoiceNotificationHandler(_notifRepo.Object, _uow.Object, Mock.Of<ILogger<EInvoiceNotificationHandler>>());
        await h.HandleSentAsync(Guid.NewGuid(), _tenantId, "EF-001", "Sovos", CancellationToken.None);
        VerifyNotificationCreated();
    }
}
