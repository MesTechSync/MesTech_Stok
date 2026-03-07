using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record PriceChangedEvent(
    Guid ProductId,
    string SKU,
    decimal OldPrice,
    decimal NewPrice,
    DateTime OccurredAt
) : IDomainEvent;
