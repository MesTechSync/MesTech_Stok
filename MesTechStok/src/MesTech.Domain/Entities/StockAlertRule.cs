using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Ürün bazlı stok alarm kuralı — akıllı eşik tanımı.
/// MinimumStock sabit threshold, StockAlertRule ise dinamik/ürün bazlı kural.
/// Örn: "iPhone 15 → 5 adet altında uyarı, 0'da kritik, otomatik sipariş aç"
/// </summary>
public sealed class StockAlertRule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? WarehouseId { get; set; }
    public int WarningThreshold { get; set; }
    public int CriticalThreshold { get; set; }
    public bool AutoReorderEnabled { get; set; }
    public int? ReorderQuantity { get; set; }
    public Guid? PreferredSupplierId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;

    private StockAlertRule() { }

    public static StockAlertRule Create(
        Guid tenantId, Guid productId, int warningThreshold, int criticalThreshold,
        Guid? warehouseId = null, bool autoReorder = false, int? reorderQty = null,
        Guid? supplierId = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentOutOfRangeException.ThrowIfNegative(warningThreshold);
        ArgumentOutOfRangeException.ThrowIfNegative(criticalThreshold);
        if (criticalThreshold >= warningThreshold)
            throw new ArgumentException("CriticalThreshold must be less than WarningThreshold", nameof(criticalThreshold));

        return new StockAlertRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            WarehouseId = warehouseId,
            WarningThreshold = warningThreshold,
            CriticalThreshold = criticalThreshold,
            AutoReorderEnabled = autoReorder,
            ReorderQuantity = reorderQty,
            PreferredSupplierId = supplierId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public StockAlertLevel EvaluateStock(int currentStock)
    {
        if (currentStock <= CriticalThreshold) return StockAlertLevel.Critical;
        if (currentStock <= WarningThreshold) return StockAlertLevel.Warning;
        return StockAlertLevel.Normal;
    }

    public bool ShouldAutoReorder(int currentStock)
        => AutoReorderEnabled && currentStock <= CriticalThreshold && ReorderQuantity > 0;
}
