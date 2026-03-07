using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record PriceChangedEvent(
    int ProductId,
    string SKU,
    decimal OldPrice,
    decimal NewPrice,
    DateTime OccurredAt
) : IDomainEvent;
