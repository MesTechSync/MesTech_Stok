using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record PaymentCompletedEvent(
    Guid TenantId,
    Guid PlatformPaymentId,
    decimal NetAmount,
    string? BankReference,
    DateTime OccurredAt
) : IDomainEvent;
