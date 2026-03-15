using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Muhasebe modulu domain olaylarinin temel sinifi.
/// Korelasyon ve nedensellik takibi icin CorrelationId/CausationId icerir.
/// </summary>
public abstract record AccountingDomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid TenantId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid CausationId { get; init; }
    public string ActorType { get; init; } = "system";
    public Guid ActorId { get; init; }
}
