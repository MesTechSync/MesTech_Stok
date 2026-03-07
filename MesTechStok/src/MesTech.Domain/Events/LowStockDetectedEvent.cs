using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record LowStockDetectedEvent(
    int ProductId,
    string SKU,
    int CurrentStock,
    int MinimumStock,
    DateTime OccurredAt
) : IDomainEvent;
