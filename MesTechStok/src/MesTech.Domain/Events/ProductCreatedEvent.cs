using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record ProductCreatedEvent(
    Guid ProductId,
    string SKU,
    string Name,
    decimal SalePrice,
    DateTime OccurredAt
) : IDomainEvent;
