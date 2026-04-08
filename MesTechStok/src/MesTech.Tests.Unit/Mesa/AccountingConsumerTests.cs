using FluentAssertions;
using MassTransit;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// 10 Accounting Consumer unit tests.
/// These consumers have richer dependency graphs (IMediator, repos, IUnitOfWork).
/// DEV5: Coverage lift 24% -> 80%+.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
public class AccountingConsumerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();

    private static Mock<IMesaEventMonitor> Monitor() => new();
    private static Mock<IMediator> Mediator() => new();
    private static Mock<IUnitOfWork> UnitOfWork() => new();
    private static Mock<ITenantProvider> TenantProvider()
    {
        var m = new Mock<ITenantProvider>();
        m.Setup(x => x.GetCurrentTenantId()).Returns(TestTenantId);
        return m;
    }

    private static ILogger<T> Logger<T>() => new Mock<ILogger<T>>().Object;

    // ══════════════════════════════════════════════
    //  AccountingApprovalConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AccountingApproval_Consume_ValidEvent_DoesNotThrow()
    {
        var mediator = Mediator();
        var docRepo = new Mock<IAccountingDocumentRepository>();
        var journalRepo = new Mock<IJournalEntryRepository>();
        var expenseRepo = new Mock<IPersonalExpenseRepository>();
        var chartRepo = new Mock<IChartOfAccountsRepository>();
        var uow = UnitOfWork();
        var monitor = Monitor();

        docRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.AccountingDocument?)null);

        var consumer = new AccountingApprovalConsumer(
            mediator.Object, docRepo.Object, journalRepo.Object, expenseRepo.Object,
            chartRepo.Object, uow.Object, monitor.Object, TenantProvider().Object,
            Logger<AccountingApprovalConsumer>());

        var ctx = new Mock<ConsumeContext<BotAccountingApprovedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new BotAccountingApprovedEvent(
            Guid.NewGuid(), "admin@test.com", "WhatsApp", null,
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AccountingApproval_Consume_DocumentNotFound_RecordsError()
    {
        var mediator = Mediator();
        var docRepo = new Mock<IAccountingDocumentRepository>();
        var monitor = Monitor();

        docRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.AccountingDocument?)null);

        var consumer = new AccountingApprovalConsumer(
            mediator.Object, docRepo.Object, new Mock<IJournalEntryRepository>().Object,
            new Mock<IPersonalExpenseRepository>().Object, new Mock<IChartOfAccountsRepository>().Object,
            UnitOfWork().Object, monitor.Object, TenantProvider().Object,
            Logger<AccountingApprovalConsumer>());

        var ctx = new Mock<ConsumeContext<BotAccountingApprovedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new BotAccountingApprovedEvent(
            Guid.NewGuid(), "admin", "Panel", null,
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordError("bot.accounting.approved", It.IsAny<string>()), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  AccountingRejectionConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AccountingRejection_Consume_ValidEvent_DoesNotThrow()
    {
        var mediator = Mediator();
        var docRepo = new Mock<IAccountingDocumentRepository>();
        var monitor = Monitor();

        docRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.AccountingDocument?)null);

        var consumer = new AccountingRejectionConsumer(
            mediator.Object, docRepo.Object, monitor.Object,
            TenantProvider().Object, Logger<AccountingRejectionConsumer>());

        var ctx = new Mock<ConsumeContext<BotAccountingRejectedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new BotAccountingRejectedEvent(
            Guid.NewGuid(), "manager@test.com", "Telegram", "Tutar yanlis",
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AccountingRejection_Consume_DocumentNotFound_RecordsError()
    {
        var mediator = Mediator();
        var docRepo = new Mock<IAccountingDocumentRepository>();
        var monitor = Monitor();

        docRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.AccountingDocument?)null);

        var consumer = new AccountingRejectionConsumer(
            mediator.Object, docRepo.Object, monitor.Object,
            TenantProvider().Object, Logger<AccountingRejectionConsumer>());

        var ctx = new Mock<ConsumeContext<BotAccountingRejectedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new BotAccountingRejectedEvent(
            Guid.NewGuid(), "admin", "Panel", null,
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordError("bot.accounting.rejected", It.IsAny<string>()), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  AiAdvisoryRecommendationConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiAdvisory_Consume_RecordsConsume()
    {
        var mediator = Mediator();
        var notifRepo = new Mock<INotificationLogRepository>();
        var uow = UnitOfWork();
        var monitor = Monitor();

        var consumer = new AiAdvisoryRecommendationConsumer(
            mediator.Object, notifRepo.Object, uow.Object, monitor.Object,
            TenantProvider().Object, Logger<AiAdvisoryRecommendationConsumer>());

        var ctx = new Mock<ConsumeContext<AiAdvisoryRecommendationEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiAdvisoryRecommendationEvent(
            "CostReduction", "Kargo maliyeti yuksek", "Son 30 gun kargo %15 artti",
            "/dashboard/cargo", "High", TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("ai.advisory.recommendation"), Times.Once);
    }

    [Fact]
    public async Task AiAdvisory_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new AiAdvisoryRecommendationConsumer(
            Mediator().Object, new Mock<INotificationLogRepository>().Object,
            UnitOfWork().Object, Monitor().Object,
            TenantProvider().Object, Logger<AiAdvisoryRecommendationConsumer>());

        var ctx = new Mock<ConsumeContext<AiAdvisoryRecommendationEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiAdvisoryRecommendationEvent(
            "PriceOptimization", "Fiyat firsati", "Rakip fiyat %10 yukseldi",
            null, "Medium", TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  AiDocumentExtractedConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiDocumentExtracted_Consume_ValidEvent_DoesNotThrow()
    {
        var mediator = Mediator();
        var docRepo = new Mock<IAccountingDocumentRepository>();
        var expenseRepo = new Mock<IPersonalExpenseRepository>();
        var monitor = Monitor();

        docRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.AccountingDocument?)null);

        var consumer = new AiDocumentExtractedConsumer(
            mediator.Object, docRepo.Object, expenseRepo.Object,
            monitor.Object, TenantProvider().Object, UnitOfWork().Object,
            Logger<AiDocumentExtractedConsumer>());

        var ctx = new Mock<ConsumeContext<AiDocumentExtractedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiDocumentExtractedEvent(
            Guid.NewGuid(), "{\"vendor\":\"Test\"}", 0.95m, 1500m,
            "1234567890", "Fatura", TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AiDocumentExtracted_Consume_DocumentNotFound_RecordsError()
    {
        var docRepo = new Mock<IAccountingDocumentRepository>();
        var monitor = Monitor();

        docRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.AccountingDocument?)null);

        var consumer = new AiDocumentExtractedConsumer(
            Mediator().Object, docRepo.Object, new Mock<IPersonalExpenseRepository>().Object,
            monitor.Object, TenantProvider().Object, UnitOfWork().Object,
            Logger<AiDocumentExtractedConsumer>());

        var ctx = new Mock<ConsumeContext<AiDocumentExtractedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiDocumentExtractedEvent(
            Guid.NewGuid(), "{}", 0.5m, null, null, null,
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordError("ai.document.extracted", It.IsAny<string>()), Times.Once);
    }

    // ══════════════════════════════════════════════
    //  AiEInvoiceDraftGeneratedConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiEInvoiceDraft_Consume_RecordsConsume()
    {
        var mediator = Mediator();
        var notifRepo = new Mock<INotificationLogRepository>();
        var uow = UnitOfWork();
        var monitor = Monitor();

        var consumer = new AiEInvoiceDraftGeneratedConsumer(
            mediator.Object, notifRepo.Object, uow.Object, monitor.Object,
            TenantProvider().Object, Logger<AiEInvoiceDraftGeneratedConsumer>());

        var ctx = new Mock<ConsumeContext<AiEInvoiceDraftGeneratedIntegrationEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiEInvoiceDraftGeneratedIntegrationEvent(
            Guid.NewGuid(), "ETTN-2026-001", 2500m, TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("ai.einvoice.draft.generated"), Times.Once);
    }

    [Fact]
    public async Task AiEInvoiceDraft_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new AiEInvoiceDraftGeneratedConsumer(
            Mediator().Object, new Mock<INotificationLogRepository>().Object,
            UnitOfWork().Object, Monitor().Object,
            TenantProvider().Object, Logger<AiEInvoiceDraftGeneratedConsumer>());

        var ctx = new Mock<ConsumeContext<AiEInvoiceDraftGeneratedIntegrationEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiEInvoiceDraftGeneratedIntegrationEvent(
            Guid.NewGuid(), "ETTN-2026-002", 100m, TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  AiErpReconciliationDoneConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiErpReconciliation_Consume_RecordsConsume()
    {
        var mediator = Mediator();
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var uow = UnitOfWork();
        var monitor = Monitor();

        var consumer = new AiErpReconciliationDoneConsumer(
            mediator.Object, matchRepo.Object, uow.Object, monitor.Object,
            TenantProvider().Object, Logger<AiErpReconciliationDoneConsumer>());

        var ctx = new Mock<ConsumeContext<AiErpReconciliationDoneIntegrationEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiErpReconciliationDoneIntegrationEvent(
            "Parasut", 100, 3, TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("ai.erp.reconciliation.done"), Times.Once);
    }

    [Fact]
    public async Task AiErpReconciliation_Consume_ZeroMismatch_DoesNotSave()
    {
        var uow = UnitOfWork();
        var consumer = new AiErpReconciliationDoneConsumer(
            Mediator().Object, new Mock<IReconciliationMatchRepository>().Object,
            uow.Object, Monitor().Object,
            TenantProvider().Object, Logger<AiErpReconciliationDoneConsumer>());

        var ctx = new Mock<ConsumeContext<AiErpReconciliationDoneIntegrationEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiErpReconciliationDoneIntegrationEvent(
            "Logo", 50, 0, TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        // Zero mismatches = no SaveChangesAsync call for reconciliation matches
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ══════════════════════════════════════════════
    //  AiReconciliationSuggestedConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task AiReconciliationSuggested_Consume_RecordsConsume()
    {
        var mediator = Mediator();
        var matchRepo = new Mock<IReconciliationMatchRepository>();
        var uow = UnitOfWork();
        var monitor = Monitor();

        var consumer = new AiReconciliationSuggestedConsumer(
            mediator.Object, matchRepo.Object, monitor.Object,
            TenantProvider().Object, uow.Object,
            Logger<AiReconciliationSuggestedConsumer>());

        var ctx = new Mock<ConsumeContext<AiReconciliationSuggestedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiReconciliationSuggestedEvent(
            Guid.NewGuid(), Guid.NewGuid(), 0.92m, "Tutar ve tarih eslesti",
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("ai.reconciliation.suggested"), Times.Once);
    }

    [Fact]
    public async Task AiReconciliationSuggested_Consume_ValidEvent_DoesNotThrow()
    {
        var consumer = new AiReconciliationSuggestedConsumer(
            Mediator().Object, new Mock<IReconciliationMatchRepository>().Object,
            Monitor().Object, TenantProvider().Object, UnitOfWork().Object,
            Logger<AiReconciliationSuggestedConsumer>());

        var ctx = new Mock<ConsumeContext<AiReconciliationSuggestedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiReconciliationSuggestedEvent(
            null, null, 0.50m, null, TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  BotEFaturaRequestedConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task BotEFatura_Consume_RecordsConsume()
    {
        var mediator = Mediator();
        var notifRepo = new Mock<INotificationLogRepository>();
        var uow = UnitOfWork();
        var monitor = Monitor();

        var consumer = new BotEFaturaRequestedConsumer(
            mediator.Object, notifRepo.Object, uow.Object, monitor.Object,
            TenantProvider().Object, Logger<BotEFaturaRequestedConsumer>());

        var ctx = new Mock<ConsumeContext<BotEFaturaRequestedIntegrationEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new BotEFaturaRequestedIntegrationEvent(
            "bot-user-123", Guid.NewGuid(), "1234567890",
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("bot.efatura.requested"), Times.Once);
    }

    [Fact]
    public async Task BotEFatura_Consume_NullOptionalFields_DoesNotThrow()
    {
        var consumer = new BotEFaturaRequestedConsumer(
            Mediator().Object, new Mock<INotificationLogRepository>().Object,
            UnitOfWork().Object, Monitor().Object,
            TenantProvider().Object, Logger<BotEFaturaRequestedConsumer>());

        var ctx = new Mock<ConsumeContext<BotEFaturaRequestedIntegrationEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new BotEFaturaRequestedIntegrationEvent(
            "bot-user-456", null, null, TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  DocumentClassifiedConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task DocumentClassified_Consume_RecordsConsume()
    {
        var mediator = Mediator();
        var docRepo = new Mock<IAccountingDocumentRepository>();
        var monitor = Monitor();

        docRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.AccountingDocument?)null);

        var consumer = new DocumentClassifiedConsumer(
            mediator.Object, docRepo.Object, UnitOfWork().Object,
            monitor.Object, TenantProvider().Object, Logger<DocumentClassifiedConsumer>());

        var ctx = new Mock<ConsumeContext<AiDocumentClassifiedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiDocumentClassifiedEvent(
            Guid.NewGuid(), "Fatura", 0.97m, 2500m, "9876543210",
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("ai.document.classified"), Times.Once);
    }

    [Fact]
    public async Task DocumentClassified_Consume_NullOptionalFields_DoesNotThrow()
    {
        var docRepo = new Mock<IAccountingDocumentRepository>();
        docRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.AccountingDocument?)null);

        var consumer = new DocumentClassifiedConsumer(
            Mediator().Object, docRepo.Object, UnitOfWork().Object,
            Monitor().Object, TenantProvider().Object, Logger<DocumentClassifiedConsumer>());

        var ctx = new Mock<ConsumeContext<AiDocumentClassifiedEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new AiDocumentClassifiedEvent(
            Guid.NewGuid(), "Makbuz", 0.60m, null, null,
            TestTenantId, DateTime.UtcNow));
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }

    // ══════════════════════════════════════════════
    //  NotificationSentConsumer
    // ══════════════════════════════════════════════

    [Fact]
    public async Task NotificationSent_Consume_Success_RecordsConsume()
    {
        var mediator = Mediator();
        var notifRepo = new Mock<INotificationLogRepository>();
        var uow = UnitOfWork();
        var monitor = Monitor();

        var consumer = new NotificationSentConsumer(
            mediator.Object, notifRepo.Object, uow.Object, monitor.Object,
            Logger<NotificationSentConsumer>());

        var ctx = new Mock<ConsumeContext<BotNotificationSentEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new BotNotificationSentEvent
        {
            TenantId = TestTenantId,
            Channel = "whatsapp",
            Recipient = "+905551234567",
            TemplateName = "order_confirmation",
            Content = "Siparisimiz onaylandi",
            Success = true,
            ErrorMessage = null,
            SentAt = DateTime.UtcNow
        });
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("bot.notification.sent"), Times.Once);
    }

    [Fact]
    public async Task NotificationSent_Consume_Failure_RecordsConsume()
    {
        var mediator = Mediator();
        var notifRepo = new Mock<INotificationLogRepository>();
        var uow = UnitOfWork();
        var monitor = Monitor();

        var consumer = new NotificationSentConsumer(
            mediator.Object, notifRepo.Object, uow.Object, monitor.Object,
            Logger<NotificationSentConsumer>());

        var ctx = new Mock<ConsumeContext<BotNotificationSentEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new BotNotificationSentEvent
        {
            TenantId = TestTenantId,
            Channel = "telegram",
            Recipient = "@testuser",
            TemplateName = "stock_alert",
            Content = "Stok kritik seviyede",
            Success = false,
            ErrorMessage = "Telegram API timeout",
            SentAt = DateTime.UtcNow
        });
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(ctx.Object);

        monitor.Verify(m => m.RecordConsume("bot.notification.sent"), Times.Once);
    }

    [Fact]
    public async Task NotificationSent_Consume_UnknownChannel_DoesNotThrow()
    {
        var consumer = new NotificationSentConsumer(
            Mediator().Object, new Mock<INotificationLogRepository>().Object,
            UnitOfWork().Object, Monitor().Object,
            Logger<NotificationSentConsumer>());

        var ctx = new Mock<ConsumeContext<BotNotificationSentEvent>>();
        ctx.SetupGet(c => c.Message).Returns(new BotNotificationSentEvent
        {
            TenantId = TestTenantId,
            Channel = "unknown_channel",
            Recipient = "test",
            TemplateName = "test",
            Content = "test",
            Success = true,
            SentAt = DateTime.UtcNow
        });
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var act = async () => await consumer.Consume(ctx.Object);
        await act.Should().NotThrowAsync();
    }
}
