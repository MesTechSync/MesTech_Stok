using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record ProductUpdatedEvent(
    Guid ProductId,
    string SKU,
    DateTime OccurredAt
) : IDomainEvent;
