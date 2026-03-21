using MesTech.Domain.Common;
using MesTech.Domain.Entities.Billing;

namespace MesTech.Domain.Events;

public record SubscriptionCreatedEvent(
    Guid TenantId,
    Guid SubscriptionId,
    Guid PlanId,
    SubscriptionStatus Status,
    DateTime OccurredAt
) : IDomainEvent;
