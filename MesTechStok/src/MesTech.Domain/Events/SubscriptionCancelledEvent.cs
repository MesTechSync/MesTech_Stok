using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record SubscriptionCancelledEvent(
    Guid TenantId,
    Guid SubscriptionId,
    string? Reason,
    DateTime OccurredAt
) : IDomainEvent;
