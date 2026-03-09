using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

public record ReturnCreatedEvent(
    Guid ReturnRequestId,
    Guid OrderId,
    PlatformType Platform,
    ReturnReason Reason,
    DateTime OccurredAt
) : IDomainEvent;
