using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Stok yetersiz sipariş girişiminde fırlatılır (InsufficientStockException öncesi).
/// Monitoring + bildirim + audit trail amaçlı — Z15 overselling koruma zinciri.
/// Handler: NotificationLog kaydı + MESA OS alarm.
/// </summary>
public record OversellingAttemptedEvent(
    Guid ProductId,
    Guid TenantId,
    string SKU,
    int AvailableStock,
    int RequestedQuantity,
    string? OrderNumber,
    DateTime OccurredAt) : IDomainEvent;
