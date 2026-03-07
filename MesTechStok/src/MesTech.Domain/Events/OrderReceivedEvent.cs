using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record OrderReceivedEvent(
    Guid OrderId,
    string PlatformCode,
    string PlatformOrderId,
    decimal TotalAmount,
    DateTime OccurredAt
) : IDomainEvent;
