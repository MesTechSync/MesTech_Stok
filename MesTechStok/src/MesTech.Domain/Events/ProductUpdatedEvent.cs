using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record ProductUpdatedEvent(
    int ProductId,
    string SKU,
    DateTime OccurredAt
) : IDomainEvent;
