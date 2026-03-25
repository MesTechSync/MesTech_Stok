using MediatR;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// InvoiceCreatedEvent → IntegrationEventPublisher.
/// Fatura oluşturulduğunda entegrasyonları bildirir.
/// </summary>
public sealed class InvoiceCreatedIntegrationHandler
    : INotificationHandler<DomainEventNotification<InvoiceCreatedEvent>>
{
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ILogger<InvoiceCreatedIntegrationHandler> _logger;

    public InvoiceCreatedIntegrationHandler(
        IIntegrationEventPublisher publisher,
        ILogger<InvoiceCreatedIntegrationHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<InvoiceCreatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "InvoiceCreated dispatch: Invoice for Order {OrderId}, total={Total}",
            e.OrderId, e.GrandTotal);

        await _publisher.PublishInvoiceCreatedAsync(
            e.InvoiceId, e.OrderId, string.Empty, e.GrandTotal).ConfigureAwait(false);
    }
}
