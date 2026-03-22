using MassTransit;
using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting;

/// <summary>
/// Muhasebe domain event'lerini dinler ve MESA muhasebe integration event'lerine
/// donusturup RabbitMQ'ya publish eder.
/// DomainEventNotification wrapper kullanir — Domain katmani INotification bilmez.
/// IMesaEventPublisher'a dokunmaz — IPublishEndpoint direkt kullanilir (ADDITIVE).
/// </summary>
public class SettlementImportedBridgeHandler
    : INotificationHandler<DomainEventNotification<SettlementImportedEvent>>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SettlementImportedBridgeHandler> _logger;

    public SettlementImportedBridgeHandler(
        IPublishEndpoint publishEndpoint,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<SettlementImportedBridgeHandler> logger)
    {
        _publishEndpoint = publishEndpoint;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<SettlementImportedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] SettlementImported yakalandi: BatchId={BatchId}, Platform={Platform}",
            e.SettlementBatchId, e.Platform);

        var tenantId = e.TenantId != Guid.Empty
            ? e.TenantId
            : _tenantProvider.GetCurrentTenantId();

        var integrationEvent = new FinanceSettlementImportedEvent(
            e.SettlementBatchId,
            e.Platform,
            e.PeriodStart,
            e.PeriodEnd,
            e.TotalNet,
            0, // LineCount domain event'te mevcut degil — 0 default
            tenantId,
            e.OccurredAt);

        await _publishEndpoint.Publish(integrationEvent, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[MESA] FinanceSettlementImported yayinlandi: batch={BatchId}, platform={Platform} (Tenant: {TenantId})",
            integrationEvent.SettlementBatchId, integrationEvent.Platform, integrationEvent.TenantId);

        _monitor.RecordPublish("finance.settlement.imported");
    }
}

public class DocumentReceivedBridgeHandler
    : INotificationHandler<DomainEventNotification<DocumentReceivedEvent>>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DocumentReceivedBridgeHandler> _logger;

    public DocumentReceivedBridgeHandler(
        IPublishEndpoint publishEndpoint,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<DocumentReceivedBridgeHandler> logger)
    {
        _publishEndpoint = publishEndpoint;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<DocumentReceivedEvent> wrapper, CancellationToken ct)
    {
        var e = wrapper.DomainEvent;
        _logger.LogDebug(
            "[MESA Bridge] DocumentReceived yakalandi: DocumentId={DocumentId}, FileName={FileName}",
            e.DocumentId, e.FileName);

        var tenantId = e.TenantId != Guid.Empty
            ? e.TenantId
            : _tenantProvider.GetCurrentTenantId();

        var integrationEvent = new FinanceDocumentReceivedEvent(
            e.DocumentId,
            e.FileName,
            string.Empty, // MimeType domain event'te mevcut degil
            e.Source.ToString(),
            tenantId,
            e.OccurredAt);

        await _publishEndpoint.Publish(integrationEvent, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[MESA] FinanceDocumentReceived yayinlandi: doc={DocumentId}, dosya={FileName} (Tenant: {TenantId})",
            integrationEvent.DocumentId, integrationEvent.FileName, integrationEvent.TenantId);

        _monitor.RecordPublish("finance.document.received");
    }
}
