using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record PaymentFailedEvent(
    Guid TenantId,
    Guid SubscriptionId,
    string? ErrorMessage,
    string? ErrorCode,
    int FailureCount,
    DateTime OccurredAt
) : IDomainEvent;
