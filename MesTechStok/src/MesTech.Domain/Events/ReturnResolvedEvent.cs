using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

public record ReturnResolvedEvent(
    Guid ReturnRequestId,
    Guid OrderId,
    ReturnStatus FinalStatus,
    decimal RefundAmount,
    DateTime OccurredAt
) : IDomainEvent;
