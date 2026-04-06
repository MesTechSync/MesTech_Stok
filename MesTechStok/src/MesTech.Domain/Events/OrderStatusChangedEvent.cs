using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

public record OrderStatusChangedEvent(
    Guid OrderId,
    Guid TenantId,
    OrderStatus OldStatus,
    OrderStatus NewStatus,
    string? ChangedBy,
    DateTime OccurredAt
) : IDomainEvent;
