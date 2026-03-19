using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Kritik stok alarm olayı — Low/Critical/OutOfStock 3 seviye.
/// LowStockDetectedEvent'ten farkı: StockAlertLevel + WarehouseId + WarehouseName bilgisi taşır.
/// MESA OS entegrasyonu için RabbitMQ'ya yayılır.
/// </summary>
public record StockCriticalEvent(
    Guid ProductId,
    string ProductName,
    string SKU,
    int CurrentStock,
    int MinimumStock,
    StockAlertLevel Level,
    Guid? WarehouseId,
    string? WarehouseName,
    DateTime OccurredAt
) : IDomainEvent;

/// <summary>Stok alarm seviyesi.</summary>
public enum StockAlertLevel
{
    /// <summary>Stok &lt;= MinimumStock × 2</summary>
    Low,
    /// <summary>Stok &lt;= MinimumStock</summary>
    Critical,
    /// <summary>Stok = 0</summary>
    OutOfStock
}
