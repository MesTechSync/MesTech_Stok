using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Ürün pasif edildiğinde yayınlanır.
/// Platform sync tetikleyici — pasif ürünler satıştan çekilir.
/// </summary>
public record ProductDeactivatedEvent(
    Guid ProductId,
    Guid TenantId,
    string SKU,
    DateTime OccurredAt
) : IDomainEvent;
