using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record OrderPlacedEvent(
    int OrderId,
    string OrderNumber,
    int CustomerId,
    decimal TotalAmount,
    DateTime OccurredAt
) : IDomainEvent;
