using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record CustomerCreatedEvent(
    Guid CustomerId,
    Guid TenantId,
    string CustomerName,
    string? Email,
    string? Phone,
    DateTime OccurredAt) : IDomainEvent;
