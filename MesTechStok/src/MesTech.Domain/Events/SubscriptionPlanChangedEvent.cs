using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record SubscriptionPlanChangedEvent(
    Guid TenantId,
    Guid SubscriptionId,
    Guid PreviousPlanId,
    Guid NewPlanId,
    DateTime OccurredAt
) : IDomainEvent;
