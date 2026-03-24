using MesTech.Domain.Common;

namespace MesTech.Domain.Events.Crm;

public record DealLostEvent(
    Guid DealId,
    Guid TenantId,
    string Reason,
    DateTime OccurredAt
) : IDomainEvent;
