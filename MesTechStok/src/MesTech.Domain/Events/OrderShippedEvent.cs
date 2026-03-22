using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

public record OrderShippedEvent(
    Guid OrderId,
    Guid TenantId,
    string TrackingNumber,
    CargoProvider CargoProvider,
    DateTime OccurredAt
) : IDomainEvent;
