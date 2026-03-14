using MediatR;
using MesTech.Domain.Events.Crm;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// Domain event DealWonEvent → MESA integration event yayını.
/// MESA Bot bu event'i alınca WhatsApp üzerinden satıcıya tebrik gönderir.
/// </summary>
public class DealWonBridgeHandler : INotificationHandler<DomainEventNotification<DealWonEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;
    private readonly ILogger<DealWonBridgeHandler> _logger;

    public DealWonBridgeHandler(
        IMesaEventPublisher mesaPublisher,
        ILogger<DealWonBridgeHandler> logger)
    {
        _mesaPublisher = mesaPublisher;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<DealWonEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        _logger.LogInformation(
            "DealWon bridge: DealId={DealId}, Amount={Amount}",
            e.DealId, e.Amount);

        await _mesaPublisher.PublishDealWonAsync(new DealWonIntegrationEvent(
            DealId: e.DealId,
            DealTitle: string.Empty,    // H27'de Deal entity'den title çekilecek
            Amount: e.Amount,
            OrderId: e.OrderId,
            CrmContactId: null,         // H27'de Deal'den ContactId alınacak
            TenantId: Guid.Empty,       // H27'de ITenantProvider inject edilecek
            OccurredAt: e.OccurredAt
        ), cancellationToken);
    }
}
