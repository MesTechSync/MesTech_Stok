using MesTech.Domain.Common;

namespace MesTech.Domain.Events.Crm;

public record LeadScoredEvent(
    Guid LeadId,
    Guid TenantId,
    int Score,
    string Reasoning,
    DateTime OccurredAt
) : IDomainEvent;
