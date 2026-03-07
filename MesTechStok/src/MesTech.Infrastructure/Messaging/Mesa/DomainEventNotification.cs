using MediatR;
using MesTech.Domain.Common;

namespace MesTech.Infrastructure.Messaging.Mesa;

/// <summary>
/// Domain event'leri MediatR INotification'a saran wrapper.
/// Domain katmani sifir NuGet bagimlilik kuralini korur —
/// IDomainEvent, INotification'i implement etmez.
/// Bu wrapper Infrastructure katmaninda kalir ve MediatR handler'larin
/// domain event'leri yakalamasini saglar.
/// </summary>
public class DomainEventNotification<TEvent> : INotification
    where TEvent : IDomainEvent
{
    public TEvent DomainEvent { get; }

    public DomainEventNotification(TEvent domainEvent)
    {
        DomainEvent = domainEvent ?? throw new ArgumentNullException(nameof(domainEvent));
    }
}
