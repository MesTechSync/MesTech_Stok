using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record ProductCreatedEvent(
    Guid ProductId,
    Guid TenantId,
    string SKU,
    string Name,
    decimal SalePrice,
    DateTime OccurredAt
) : IDomainEvent;
