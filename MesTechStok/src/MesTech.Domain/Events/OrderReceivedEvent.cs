using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record OrderReceivedEvent(
    Guid OrderId,
    Guid TenantId,
    string PlatformCode,
    string PlatformOrderId,
    decimal TotalAmount,
    DateTime OccurredAt
) : IDomainEvent;
