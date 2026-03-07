using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record LowStockDetectedEvent(
    Guid ProductId,
    string SKU,
    int CurrentStock,
    int MinimumStock,
    DateTime OccurredAt
) : IDomainEvent;
