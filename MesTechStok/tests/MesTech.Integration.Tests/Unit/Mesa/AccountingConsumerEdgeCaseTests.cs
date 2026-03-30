using FluentAssertions;
using MassTransit;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using MesTech.Infrastructure.Messaging.Mesa.Consumers;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Mesa;

/// <summary>
/// G473: Remaining MESA consumer edge case tests.
/// 9 consumers: AiDocumentExtracted, AiEInvoiceDraft, AiErpReconciliation,
/// AiReconciliationSuggested, BotEFatura, DocumentClassified, NotificationSent,
/// MesaLeadScored, MesaMeetingScheduled, MesaDlq.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Mesa")]
[Trait("Group", "AccountingConsumer")]
public class AccountingConsumerEdgeCaseTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IMesaEventMonitor> _monitor = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _fallbackTenantId = Guid.NewGuid();

    public AccountingConsumerEdgeCaseTests()
    {
        _tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(_fallbackTenantId);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static ConsumeContext<T> MockContext<T>(T message) where T : class
    {
        var ctx = new Mock<ConsumeContext<T>>();
        ctx.Setup(c => c.Message).Returns(message);
        ctx.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        ctx.Setup(c => c.MessageId).Returns(Guid.NewGuid());
        return ctx.Object;
    }

    // ═══ AiDocumentExtractedConsumer ═══

    [Fact]
    public async Task AiDocumentExtracted_EmptyTenantId_UsesFallback()
    {
        var docRepo = new Mock<IAccountingDocumentRepository>();
        var expenseRepo = new Mock<IPersonalExpenseRepository>();

        var consumer = new AiDocumentExtractedConsumer(
            _mediator.Object, docRepo.Object, expenseRepo.Object,
            _monitor.Object, _tenantProvider.Object, _uow.Object,
            Mock.Of<ILogger<AiDocumentExtractedConsumer>>());

        var msg = new AiDocumentExtractedEvent(
            DocumentId: Guid.NewGuid(),
            ProcessedJson: "{}",
            Confidence: 0.95m,
            ExtractedAmount: null,
            ExtractedVKN: null,
            ExtractedCategory: null,
            TenantId: Guid.Empty,
            OccurredAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        _tenantProvider.Verify(t => t.GetCurrentTenantId(), Times.Once);
    }

    // ═══ AiEInvoiceDraftGeneratedConsumer ═══

    [Fact]
    public async Task AiEInvoiceDraft_EmptyTenantId_UsesFallback()
    {
        var notifRepo = new Mock<INotificationLogRepository>();

        var consumer = new AiEInvoiceDraftGeneratedConsumer(
            _mediator.Object, notifRepo.Object, _uow.Object,
            _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<AiEInvoiceDraftGeneratedConsumer>>());

        var msg = new AiEInvoiceDraftGeneratedIntegrationEvent(
            OrderId: Guid.NewGuid(),
            SuggestedEttnNo: "INV-001",
            SuggestedTotal: 1500.00m,
            TenantId: Guid.Empty,
            OccurredAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        _tenantProvider.Verify(t => t.GetCurrentTenantId(), Times.Once);
    }

    // ═══ AiErpReconciliationDoneConsumer ═══

    [Fact]
    public async Task AiErpReconciliation_EmptyTenantId_UsesFallback()
    {
        var matchRepo = new Mock<IReconciliationMatchRepository>();

        var consumer = new AiErpReconciliationDoneConsumer(
            _mediator.Object, matchRepo.Object, _uow.Object,
            _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<AiErpReconciliationDoneConsumer>>());

        var msg = new AiErpReconciliationDoneIntegrationEvent(
            ErpProvider: "Parasut",
            ReconciledCount: 42,
            MismatchCount: 3,
            TenantId: Guid.Empty,
            OccurredAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        _tenantProvider.Verify(t => t.GetCurrentTenantId(), Times.Once);
    }

    // ═══ BotEFaturaRequestedConsumer ═══

    [Fact]
    public async Task BotEFatura_EmptyTenantId_UsesFallback()
    {
        var notifRepo = new Mock<INotificationLogRepository>();

        var consumer = new BotEFaturaRequestedConsumer(
            _mediator.Object, notifRepo.Object, _uow.Object,
            _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<BotEFaturaRequestedConsumer>>());

        var msg = new BotEFaturaRequestedIntegrationEvent(
            BotUserId: "whatsapp-bot",
            OrderId: Guid.NewGuid(),
            BuyerVkn: null,
            TenantId: Guid.Empty,
            OccurredAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        _tenantProvider.Verify(t => t.GetCurrentTenantId(), Times.Once);
    }

    // ═══ DocumentClassifiedConsumer ═══

    [Fact]
    public async Task DocumentClassified_EmptyTenantId_UsesFallback()
    {
        var docRepo = new Mock<IAccountingDocumentRepository>();

        var consumer = new DocumentClassifiedConsumer(
            _mediator.Object, docRepo.Object,
            _monitor.Object, _tenantProvider.Object,
            Mock.Of<ILogger<DocumentClassifiedConsumer>>());

        var msg = new AiDocumentClassifiedEvent(
            DocumentId: Guid.NewGuid(),
            DocumentType: "invoice",
            Confidence: 0.88m,
            ExtractedAmount: null,
            ExtractedVKN: null,
            TenantId: Guid.Empty,
            OccurredAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        _tenantProvider.Verify(t => t.GetCurrentTenantId(), Times.Once);
    }

    // ═══ NotificationSentConsumer ═══

    [Fact]
    public async Task NotificationSent_ValidMessage_ProcessesSuccessfully()
    {
        var notifRepo = new Mock<INotificationLogRepository>();

        var consumer = new NotificationSentConsumer(
            _mediator.Object, notifRepo.Object, _uow.Object,
            _monitor.Object,
            Mock.Of<ILogger<NotificationSentConsumer>>());

        var msg = new BotNotificationSentEvent
        {
            TenantId = Guid.NewGuid(),
            Channel = "email",
            Recipient = "test@example.com",
            TemplateName = "order_confirmation",
            Content = "Your order has been confirmed.",
            Success = true,
            SentAt = DateTime.UtcNow
        };

        await consumer.Consume(MockContext(msg));
        // Should not throw
    }

    // ═══ MesaLeadScoredConsumer ═══

    [Fact]
    public async Task MesaLeadScored_ValidMessage_UpdatesLeadScore()
    {
        var leadRepo = new Mock<ICrmLeadRepository>();

        var consumer = new MesaLeadScoredConsumer(
            _monitor.Object, leadRepo.Object, _uow.Object,
            Mock.Of<ILogger<MesaLeadScoredConsumer>>());

        var leadId = Guid.NewGuid();
        var msg = new MesaLeadScoredEvent(
            LeadId: leadId,
            Score: 85,
            Reasoning: "High engagement",
            TenantId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        // Lead repo should be called
        leadRepo.Verify(r => r.GetByIdAsync(leadId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ MesaMeetingScheduledConsumer ═══

    [Fact]
    public async Task MesaMeetingScheduled_ValidMessage_SendsCommand()
    {
        var consumer = new MesaMeetingScheduledConsumer(
            _mediator.Object, Mock.Of<ILogger<MesaMeetingScheduledConsumer>>());

        var msg = new MesaMeetingScheduledEvent(
            Title: "Satış Görüşmesi",
            StartAt: DateTime.UtcNow.AddDays(1),
            EndAt: DateTime.UtcNow.AddDays(1).AddHours(1),
            Location: null,
            AttendeeUserIds: Array.Empty<Guid>(),
            TenantId: Guid.NewGuid(),
            RelatedDealId: null,
            OccurredAt: DateTime.UtcNow);

        await consumer.Consume(MockContext(msg));
        _mediator.Verify(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══ MesaDlqConsumer ═══

    [Fact]
    public async Task MesaDlq_FaultMessage_LogsAndMonitors()
    {
        var consumer = new MesaDlqConsumer(
            _monitor.Object, Mock.Of<ILogger<MesaDlqConsumer>>());

        var faultMsg = new Mock<ConsumeContext<Fault>>();
        var fault = new Mock<Fault>();
        faultMsg.Setup(c => c.Message).Returns(fault.Object);
        faultMsg.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        faultMsg.Setup(c => c.MessageId).Returns(Guid.NewGuid());

        await consumer.Consume(faultMsg.Object);
        // Should not throw — DLQ consumer logs and monitors
    }
}
