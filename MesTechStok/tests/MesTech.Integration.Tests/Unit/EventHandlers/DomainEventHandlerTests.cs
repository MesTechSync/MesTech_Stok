using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using IJournalEntryRepository = MesTech.Application.Interfaces.Accounting.IJournalEntryRepository;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.EventHandlers;

/// <summary>
/// Domain Event Handler testleri — 15 zincirin kalbi.
/// Z1: Sipariş→Stok düşür, Z2: Sipariş→Gelir kaydı,
/// Z5: İade→Stok geri, Z6: Komisyon→GL, Z7: Kargo→GL,
/// Z8: Stok 0→Platform pasif, Z10: Zarar uyarısı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "EventHandler")]
[Trait("Group", "DomainEvent")]
public class DomainEventHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<IUnitOfWork> _uow = new();

    public DomainEventHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ═══ Z2: OrderConfirmedRevenue — sipariş onayı → gelir kaydı ═══

    [Fact]
    public async Task OrderConfirmedRevenue_CreatesIncome()
    {
        var incomeRepo = new Mock<IIncomeRepository>();
        incomeRepo.Setup(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new OrderConfirmedRevenueHandler(incomeRepo.Object, _uow.Object);
        await handler.HandleAsync(Guid.NewGuid(), _tenantId, "ORD-001", 5000m, Guid.NewGuid(), CancellationToken.None);

        incomeRepo.Verify(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ Z6: CommissionCharged → GL yevmiye (760.02 borç, 120 alacak) ═══

    [Fact]
    public async Task CommissionChargedGL_CreatesJournalEntry()
    {
        var journalRepo = new Mock<IJournalEntryRepository>();
        journalRepo.Setup(r => r.AddAsync(It.IsAny<MesTech.Domain.Accounting.Entities.JournalEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var logger = Mock.Of<ILogger<CommissionChargedGLHandler>>();

        var handler = new CommissionChargedGLHandler(_uow.Object, journalRepo.Object, logger);
        await handler.HandleAsync(
            Guid.NewGuid(), _tenantId, "Trendyol", 500m, 0.10m, CancellationToken.None);

        journalRepo.Verify(r => r.AddAsync(
            It.IsAny<MesTech.Domain.Accounting.Entities.JournalEntry>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ Z3: InvoiceApproved → GL yevmiye (120 borç, 600+391 alacak) ═══

    [Fact]
    public async Task InvoiceApprovedGL_CreatesJournalEntry()
    {
        var journalRepo = new Mock<IJournalEntryRepository>();
        journalRepo.Setup(r => r.AddAsync(It.IsAny<MesTech.Domain.Accounting.Entities.JournalEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var logger = Mock.Of<ILogger<InvoiceApprovedGLHandler>>();

        var handler = new InvoiceApprovedGLHandler(_uow.Object, journalRepo.Object, logger);
        await handler.HandleAsync(
            Guid.NewGuid(), _tenantId, "INV-001", 11800m, 1800m, 10000m, CancellationToken.None);

        journalRepo.Verify(r => r.AddAsync(
            It.IsAny<MesTech.Domain.Accounting.Entities.JournalEntry>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ Z7: OrderShippedCost → GL yevmiye (760.01 borç, 320 alacak) ═══

    [Fact]
    public async Task OrderShippedCost_CreatesJournalEntry()
    {
        var journalRepo = new Mock<IJournalEntryRepository>();
        journalRepo.Setup(r => r.AddAsync(It.IsAny<MesTech.Domain.Accounting.Entities.JournalEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var logger = Mock.Of<ILogger<OrderShippedCostHandler>>();

        var handler = new OrderShippedCostHandler(_uow.Object, journalRepo.Object, logger);
        await handler.HandleAsync(
            Guid.NewGuid(), _tenantId, "TRK-001", "Yurtici", 45.50m, CancellationToken.None);

        journalRepo.Verify(r => r.AddAsync(
            It.IsAny<MesTech.Domain.Accounting.Entities.JournalEntry>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ Z10: PriceLossDetected → Notification ═══

    [Fact]
    public async Task PriceLossDetected_CreatesNotification()
    {
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new PriceLossDetectedEventHandler(notifRepo.Object, _uow.Object);
        await handler.HandleAsync(
            Guid.NewGuid(), _tenantId, "SKU-001", 100m, 80m, 20m, CancellationToken.None);

        notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ Z8: ZeroStockDetected → Notification ═══

    [Fact]
    public async Task ZeroStockDetected_CreatesNotification()
    {
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ZeroStockDetectedEventHandler(notifRepo.Object, _uow.Object);
        await handler.HandleAsync(
            Guid.NewGuid(), _tenantId, "SKU-ZERO", CancellationToken.None);

        notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ LowStock → Notification ═══

    [Fact]
    public async Task LowStockNotification_CreatesNotification()
    {
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new LowStockNotificationHandler(
            notifRepo.Object, _uow.Object, Mock.Of<ILogger<LowStockNotificationHandler>>());
        await handler.HandleAsync(
            Guid.NewGuid(), _tenantId, "SKU-LOW", 3, 10, CancellationToken.None);

        notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ OrderReceivedNotification ═══

    [Fact]
    public async Task OrderReceivedNotification_CreatesNotification()
    {
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new OrderReceivedNotificationHandler(
            notifRepo.Object, _uow.Object, Mock.Of<ILogger<OrderReceivedNotificationHandler>>());
        await handler.HandleAsync(
            Guid.NewGuid(), _tenantId, "Trendyol", "PLT-123", 1500m, CancellationToken.None);

        notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ SyncErrorNotification ═══

    [Fact]
    public async Task SyncErrorNotification_CreatesNotification()
    {
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new SyncErrorNotificationHandler(
            notifRepo.Object, _uow.Object, Mock.Of<ILogger<SyncErrorNotificationHandler>>());
        await handler.HandleAsync(
            _tenantId, "Trendyol", "CONNECTION_TIMEOUT", "Bağlantı zaman aşımı", CancellationToken.None);

        notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
