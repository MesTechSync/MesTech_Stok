using MediatR;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// OrderReceivedEvent → IntegrationEventPublisher (platform sync).
/// Platform siparişi alındığında entegrasyonları bildirir.
/// </summary>
public sealed class OrderReceivedIntegrationHandler
    : INotificationHandler<DomainEventNotification<OrderReceivedEvent>>
{
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ILogger<OrderReceivedIntegrationHandler> _logger;

    public OrderReceivedIntegrationHandler(
        IIntegrationEventPublisher publisher,
        ILogger<OrderReceivedIntegrationHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<OrderReceivedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "OrderReceived dispatch: {Platform} #{OrderId} total={Total}",
            e.PlatformCode, e.PlatformOrderId, e.TotalAmount);

        await _publisher.PublishOrderReceivedAsync(
            e.OrderId, e.PlatformCode, e.PlatformOrderId, e.TotalAmount, cancellationToken).ConfigureAwait(false);
    }
}
