using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record OrderPlacedEvent(
    Guid OrderId,
    Guid TenantId,
    string OrderNumber,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime OccurredAt
) : IDomainEvent;
