using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// Kritik stok alarm olayı — Low/Critical/OutOfStock 3 seviye.
/// LowStockDetectedEvent'ten farkı: StockAlertLevel + WarehouseId + WarehouseName bilgisi taşır.
/// MESA OS entegrasyonu için RabbitMQ'ya yayılır.
/// </summary>
public record StockCriticalEvent(
    Guid ProductId,
    Guid TenantId,
    string ProductName,
    string SKU,
    int CurrentStock,
    int MinimumStock,
    StockAlertLevel Level,
    Guid? WarehouseId,
    string? WarehouseName,
    DateTime OccurredAt
) : IDomainEvent;

