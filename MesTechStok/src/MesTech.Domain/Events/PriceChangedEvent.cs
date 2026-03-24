using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record PriceChangedEvent(
    Guid ProductId,
    Guid TenantId,
    string SKU,
    decimal OldPrice,
    decimal NewPrice,
    DateTime OccurredAt
) : IDomainEvent;
