using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record LowStockDetectedEvent(
    Guid ProductId,
    Guid TenantId,
    string SKU,
    int CurrentStock,
    int MinimumStock,
    DateTime OccurredAt
) : IDomainEvent;
