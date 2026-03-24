using MesTech.Domain.Common;

namespace MesTech.Domain.Events.Crm;

public record LeadConvertedEvent(
    Guid LeadId,
    Guid TenantId,
    Guid CrmContactId,
    DateTime OccurredAt
) : IDomainEvent;
