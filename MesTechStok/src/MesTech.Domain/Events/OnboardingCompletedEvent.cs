using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record OnboardingCompletedEvent(
    Guid TenantId,
    Guid OnboardingProgressId,
    DateTime StartedAt,
    DateTime CompletedAt,
    DateTime OccurredAt
) : IDomainEvent;
