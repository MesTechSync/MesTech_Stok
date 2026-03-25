using MediatR;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// StockChangedEvent → IntegrationEventPublisher (platform sync).
/// Stok değiştiğinde pazaryeri adapter'larına bildirim gönderir.
/// </summary>
public sealed class StockChangedIntegrationHandler
    : INotificationHandler<DomainEventNotification<StockChangedEvent>>
{
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ILogger<StockChangedIntegrationHandler> _logger;

    public StockChangedIntegrationHandler(
        IIntegrationEventPublisher publisher,
        ILogger<StockChangedIntegrationHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<StockChangedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "StockChanged dispatch: {SKU} {Prev}->{New} ({Type})",
            e.SKU, e.PreviousQuantity, e.NewQuantity, e.MovementType);

        await _publisher.PublishStockChangedAsync(
            e.ProductId, e.SKU, e.NewQuantity, e.MovementType.ToString()).ConfigureAwait(false);
    }
}
