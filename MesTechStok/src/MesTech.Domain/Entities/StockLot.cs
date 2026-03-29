using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Stok Lot/Parti kaydi — FIFO maliyet takibi, tedarikci, SKT, depo.
/// G415: StockLot entity for lot-based stock tracking.
/// </summary>
public sealed class StockLot : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; private set; }
    public string LotNumber { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public int RemainingQuantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost => RemainingQuantity * UnitCost;

    public Guid? SupplierId { get; private set; }
    public string? SupplierName { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public string? WarehouseName { get; private set; }

    public DateTime? ExpiryDate { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    public Product? Product { get; private set; }
    public Warehouse? Warehouse { get; private set; }

    private StockLot() { }

    public static StockLot Create(
        Guid tenantId, Guid productId, string lotNumber,
        int quantity, decimal unitCost,
        Guid? warehouseId = null, string? warehouseName = null,
        Guid? supplierId = null, string? supplierName = null,
        DateTime? expiryDate = null, string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lotNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitCost);

        return new StockLot
        {
            TenantId = tenantId,
            ProductId = productId,
            LotNumber = lotNumber,
            Quantity = quantity,
            RemainingQuantity = quantity,
            UnitCost = unitCost,
            WarehouseId = warehouseId,
            WarehouseName = warehouseName,
            SupplierId = supplierId,
            SupplierName = supplierName,
            ExpiryDate = expiryDate,
            ReceivedAt = DateTime.UtcNow,
            Notes = notes
        };
    }

    /// <summary>FIFO: Lot'tan miktar düş.</summary>
    public int Deduct(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        var deducted = Math.Min(amount, RemainingQuantity);
        RemainingQuantity -= deducted;
        UpdatedAt = DateTime.UtcNow;
        return deducted;
    }

    /// <summary>İade: Lot'a miktar ekle.</summary>
    public void Restore(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (RemainingQuantity + amount > Quantity)
            throw new InvalidOperationException("Iade miktari orijinal lot miktarini asamaz.");
        RemainingQuantity += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
    public bool IsFullyConsumed => RemainingQuantity <= 0;
}
