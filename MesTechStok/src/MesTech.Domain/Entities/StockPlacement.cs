using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Stok Yerlesim kaydi — urunun hangi depoda/rafta/binde bulundugu.
/// G415: StockPlacement entity for warehouse-shelf-bin stock tracking.
/// </summary>
public sealed class StockPlacement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? ShelfId { get; private set; }
    public Guid? BinId { get; private set; }

    public string? WarehouseName { get; private set; }
    public string? ShelfCode { get; private set; }
    public string? BinCode { get; private set; }

    public int Quantity { get; private set; }
    public int MinimumStock { get; private set; }

    // Product snapshot — JOIN'siz listeleme icin
    public string? ProductName { get; private set; }
    public string? ProductSku { get; private set; }

    // Navigation
    public Product? Product { get; private set; }
    public Warehouse? Warehouse { get; private set; }

    private StockPlacement() { }

    public static StockPlacement Create(
        Guid tenantId, Guid productId, Guid warehouseId,
        int quantity, int minimumStock = 0,
        Guid? shelfId = null, Guid? binId = null,
        string? warehouseName = null, string? shelfCode = null, string? binCode = null,
        string? productName = null, string? productSku = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumStock);

        return new StockPlacement
        {
            TenantId = tenantId,
            ProductId = productId,
            WarehouseId = warehouseId,
            ShelfId = shelfId,
            BinId = binId,
            Quantity = quantity,
            MinimumStock = minimumStock,
            WarehouseName = warehouseName,
            ShelfCode = shelfCode,
            BinCode = binCode,
            ProductName = productName,
            ProductSku = productSku
        };
    }

    public void AdjustQuantity(int delta)
    {
        var newQty = Quantity + delta;
        if (newQty < 0)
            throw new InvalidOperationException($"Stok negatife dusurulemez. Mevcut: {Quantity}, Delta: {delta}");
        Quantity = newQty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMinimumStock(int minimum)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimum);
        MinimumStock = minimum;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsCritical => Quantity <= MinimumStock;
    public bool IsOutOfStock => Quantity <= 0;

    public string StockStatus => Quantity switch
    {
        <= 0 => "TUKENDI",
        _ when Quantity <= MinimumStock => "KRITIK",
        _ when Quantity <= MinimumStock * 1.5m => "DUSUK",
        _ => "YETERLI"
    };
}
