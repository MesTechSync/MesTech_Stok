using MesTech.Domain.Common;

namespace MesTech.Domain.Events.Crm;

public record DealWonEvent(
    Guid DealId,
    Guid? OrderId,
    decimal Amount,
    DateTime OccurredAt
) : IDomainEvent;
