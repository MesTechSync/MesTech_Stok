using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Ürün aktif edildiğinde yayınlanır.
/// Platform sync tetikleyici — aktif ürünler satışa açılır.
/// </summary>
public record ProductActivatedEvent(
    Guid ProductId,
    Guid TenantId,
    string SKU,
    DateTime OccurredAt
) : IDomainEvent;
