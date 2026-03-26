using FluentAssertions;
using MassTransit;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using IJournalEntryRepository = MesTech.Domain.Interfaces.IJournalEntryRepository;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Mesa;

/// <summary>
/// İ-13 S-11: Derinleştirilmiş consumer'ların unit testleri.
/// 4 consumer x 3 senaryo = 12 test.
/// </summary>
public class ConsumerDepthTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<IMediator> _mediator = new();

    // ═══════════════════════════════════════════════════════════════
    // AiAdvisoryRecommendationConsumer (3 tests)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task AiAdvisory_ValidEvent_CreatesNotificationLog()
    {
        // Arrange
        var notifRepo = new Mock<INotificationLogRepository>();
        var uow = new Mock<IUnitOfWork>();
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<AiAdvisoryRecommendationConsumer>>();

        NotificationLog? captured = null;
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n)
            .Returns(Task.CompletedTask);

        var consumer = new AiAdvisoryRecommendationConsumer(
            _mediator.Object, notifRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        var evt = new AiAdvisoryRecommendationEvent(
            "CashFlowWarning", "Nakit akis uyarisi", "30 gun icinde nakit sikintisi",
            "https://dashboard/cashflow", "High", _tenantId, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.advisory.recommendation"), Times.Once);
        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task AiAdvisory_EmptyTenantId_UsesDefaultFromProvider()
    {
        // Arrange
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        var defaultTenant = Guid.NewGuid();
        tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(defaultTenant);
        var logger = new Mock<ILogger<AiAdvisoryRecommendationConsumer>>();

        var consumer = new AiAdvisoryRecommendationConsumer(
            _mediator.Object, notifRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        var evt = new AiAdvisoryRecommendationEvent(
            "TaxDeadline", "KDV bildirimi", "KDV son gun yaklasıyor",
            null, "Medium", Guid.Empty, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        tenantProvider.Verify(t => t.GetCurrentTenantId(), Times.Once);
        notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AiAdvisory_RepositoryThrows_ExceptionPropagates()
    {
        // Arrange
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB write failed"));
        var uow = new Mock<IUnitOfWork>();
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<AiAdvisoryRecommendationConsumer>>();

        var consumer = new AiAdvisoryRecommendationConsumer(
            _mediator.Object, notifRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        var evt = new AiAdvisoryRecommendationEvent(
            "Alert", "Test", "Test", null, "Low", _tenantId, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act & Assert — exception propagates for MassTransit retry
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => consumer.Consume(context.Object));
    }

    // ═══════════════════════════════════════════════════════════════
    // AiEInvoiceDraftGeneratedConsumer (3 tests)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task EInvoiceDraft_ValidEvent_CreatesNotificationForAccounting()
    {
        // Arrange
        var notifRepo = new Mock<INotificationLogRepository>();
        NotificationLog? captured = null;
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n)
            .Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<AiEInvoiceDraftGeneratedConsumer>>();

        var consumer = new AiEInvoiceDraftGeneratedConsumer(
            _mediator.Object, notifRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        var orderId = Guid.NewGuid();
        var evt = new AiEInvoiceDraftGeneratedIntegrationEvent(
            orderId, "ETTN-2026-001", 1250.50m, _tenantId, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.einvoice.draft.generated"), Times.Once);
        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("ETTN-2026-001");
        captured.Content.Should().Contain("1250,50");
    }

    [Fact]
    public async Task EInvoiceDraft_EmptyTenantId_FallsBackToProvider()
    {
        // Arrange
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        var logger = new Mock<ILogger<AiEInvoiceDraftGeneratedConsumer>>();

        var consumer = new AiEInvoiceDraftGeneratedConsumer(
            _mediator.Object, notifRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        var evt = new AiEInvoiceDraftGeneratedIntegrationEvent(
            Guid.NewGuid(), "ETTN-X", 100m, Guid.Empty, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        tenantProvider.Verify(t => t.GetCurrentTenantId(), Times.Once);
    }

    [Fact]
    public async Task EInvoiceDraft_SaveFails_ExceptionBubbles()
    {
        // Arrange
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SaveChanges failed"));
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<AiEInvoiceDraftGeneratedConsumer>>();

        var consumer = new AiEInvoiceDraftGeneratedConsumer(
            _mediator.Object, notifRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        var evt = new AiEInvoiceDraftGeneratedIntegrationEvent(
            Guid.NewGuid(), "ETTN-Y", 200m, _tenantId, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => consumer.Consume(context.Object));
    }

    // ═══════════════════════════════════════════════════════════════
    // AiErpReconciliationDoneConsumer (3 tests)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ErpReconciliation_WithMismatches_CreatesReconciliationMatches()
    {
        // Arrange
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var addedMatches = new List<ReconciliationMatch>();
        matchRepo.Setup(r => r.AddAsync(It.IsAny<ReconciliationMatch>(), It.IsAny<CancellationToken>()))
            .Callback<ReconciliationMatch, CancellationToken>((m, _) => addedMatches.Add(m))
            .Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<AiErpReconciliationDoneConsumer>>();

        var consumer = new AiErpReconciliationDoneConsumer(
            _mediator.Object, matchRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        var evt = new AiErpReconciliationDoneIntegrationEvent(
            "Parasut", 50, 3, _tenantId, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        matchRepo.Verify(r => r.AddAsync(It.IsAny<ReconciliationMatch>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("ai.erp.reconciliation.done"), Times.Once);
        addedMatches.Should().HaveCount(3);
        addedMatches.Should().AllSatisfy(m => m.TenantId.Should().Be(_tenantId));
    }

    [Fact]
    public async Task ErpReconciliation_ZeroMismatches_NoMatchesCreated()
    {
        // Arrange
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<AiErpReconciliationDoneConsumer>>();

        var consumer = new AiErpReconciliationDoneConsumer(
            _mediator.Object, matchRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        var evt = new AiErpReconciliationDoneIntegrationEvent(
            "Parasut", 100, 0, _tenantId, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act
        await consumer.Consume(context.Object);

        // Assert — no matches created, no SaveChanges called
        matchRepo.Verify(r => r.AddAsync(It.IsAny<ReconciliationMatch>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        monitor.Verify(m => m.RecordConsume("ai.erp.reconciliation.done"), Times.Once);
    }

    [Fact]
    public async Task ErpReconciliation_RepositoryFails_ExceptionPropagates()
    {
        // Arrange
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        matchRepo.Setup(r => r.AddAsync(It.IsAny<ReconciliationMatch>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));
        var uow = new Mock<IUnitOfWork>();
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<AiErpReconciliationDoneConsumer>>();

        var consumer = new AiErpReconciliationDoneConsumer(
            _mediator.Object, matchRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        var evt = new AiErpReconciliationDoneIntegrationEvent(
            "Logo", 10, 2, _tenantId, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => consumer.Consume(context.Object));
    }

    // ═══════════════════════════════════════════════════════════════
    // BotEFaturaRequestedConsumer (3 tests)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task BotEFatura_ValidEvent_CreatesNotificationLog()
    {
        // Arrange
        var notifRepo = new Mock<INotificationLogRepository>();
        NotificationLog? captured = null;
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationLog, CancellationToken>((n, _) => captured = n)
            .Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<BotEFaturaRequestedConsumer>>();

        var consumer = new BotEFaturaRequestedConsumer(
            _mediator.Object, notifRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        var orderId = Guid.NewGuid();
        var evt = new BotEFaturaRequestedIntegrationEvent(
            "bot-user-123", orderId, "1234567890", _tenantId, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("bot.efatura.requested"), Times.Once);
        captured.Should().NotBeNull();
        captured!.Content.Should().Contain("bot-user-123");
        captured.Content.Should().Contain("1234567890");
    }

    [Fact]
    public async Task BotEFatura_NullOptionalFields_HandlesGracefully()
    {
        // Arrange
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<BotEFaturaRequestedConsumer>>();

        var consumer = new BotEFaturaRequestedConsumer(
            _mediator.Object, notifRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        // OrderId=null, BuyerVkn=null
        var evt = new BotEFaturaRequestedIntegrationEvent(
            "bot-user-456", null, null, _tenantId, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act — should not throw
        await consumer.Consume(context.Object);

        // Assert
        notifRepo.Verify(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        monitor.Verify(m => m.RecordConsume("bot.efatura.requested"), Times.Once);
    }

    [Fact]
    public async Task BotEFatura_SaveFails_ExceptionPropagatesForRetry()
    {
        // Arrange
        var notifRepo = new Mock<INotificationLogRepository>();
        notifRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection lost"));
        var monitor = new Mock<IMesaEventMonitor>();
        var tenantProvider = new Mock<ITenantProvider>();
        var logger = new Mock<ILogger<BotEFaturaRequestedConsumer>>();

        var consumer = new BotEFaturaRequestedConsumer(
            _mediator.Object, notifRepo.Object, uow.Object, monitor.Object, tenantProvider.Object, logger.Object);

        var evt = new BotEFaturaRequestedIntegrationEvent(
            "bot-user-789", Guid.NewGuid(), "9876543210", _tenantId, DateTime.UtcNow);

        var context = CreateConsumeContext(evt);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => consumer.Consume(context.Object));
    }

    // ═══════════════════════════════════════════════════════════════
    // Helper
    // ═══════════════════════════════════════════════════════════════

    private static Mock<ConsumeContext<T>> CreateConsumeContext<T>(T message) where T : class
    {
        var context = new Mock<ConsumeContext<T>>();
        context.Setup(c => c.Message).Returns(message);
        context.Setup(c => c.MessageId).Returns(Guid.NewGuid());
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }
}
