using MesTech.Domain.Common;

namespace MesTech.Domain.Events.Crm;

public record DealStageChangedEvent(
    Guid DealId,
    Guid FromStageId,
    Guid ToStageId,
    DateTime OccurredAt
) : IDomainEvent;
