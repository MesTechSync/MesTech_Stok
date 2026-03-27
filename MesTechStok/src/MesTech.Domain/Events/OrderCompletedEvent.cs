using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record OrderCompletedEvent(
    Guid OrderId,
    Guid TenantId,
    string OrderNumber,
    decimal TotalAmount,
    string? CustomerName,
    DateTime OccurredAt) : IDomainEvent;
