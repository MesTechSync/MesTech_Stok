using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record SupplierCreatedEvent(
    Guid SupplierId,
    Guid TenantId,
    string SupplierName,
    string Code,
    DateTime OccurredAt) : IDomainEvent;
