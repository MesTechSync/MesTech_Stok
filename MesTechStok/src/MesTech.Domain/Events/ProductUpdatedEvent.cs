using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record ProductUpdatedEvent(
    Guid ProductId,
    Guid TenantId,
    string SKU,
    DateTime OccurredAt
) : IDomainEvent;
