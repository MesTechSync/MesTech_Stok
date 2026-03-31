using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record CategoryCreatedEvent(
    Guid CategoryId,
    Guid TenantId,
    string CategoryName,
    string Code,
    DateTime OccurredAt) : IDomainEvent;
