using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// Stok değiştiğinde fırlatılır — entegratörlere otomatik push tetikler.
/// </summary>
public record StockChangedEvent(
    Guid ProductId,
    string SKU,
    int PreviousQuantity,
    int NewQuantity,
    StockMovementType MovementType,
    DateTime OccurredAt
) : IDomainEvent;
