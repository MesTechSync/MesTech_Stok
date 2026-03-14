using MediatR;
using MesTech.Domain.Events.Crm;
using MesTech.Infrastructure.Messaging.Mesa;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// Lead convert olunca MESA AI'ya bildir — AI lead skoru ve öneri üretebilir.
/// </summary>
public class LeadConvertedBridgeHandler : INotificationHandler<DomainEventNotification<LeadConvertedEvent>>
{
    private readonly IMesaEventPublisher _mesaPublisher;

    public LeadConvertedBridgeHandler(IMesaEventPublisher mesaPublisher)
        => _mesaPublisher = mesaPublisher;

    public async Task Handle(
        DomainEventNotification<LeadConvertedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        await _mesaPublisher.PublishLeadConvertedAsync(new LeadConvertedIntegrationEvent(
            LeadId: e.LeadId,
            CrmContactId: e.CrmContactId,
            FullName: string.Empty,   // H27'de Lead entity'den alınacak
            Email: null,
            TenantId: Guid.Empty,     // H27'de ITenantProvider inject edilecek
            OccurredAt: e.OccurredAt
        ), cancellationToken);
    }
}
