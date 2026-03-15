using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Urun bazli depo/fulfillment center stok kaydi.
/// Ayni urunun farkli depo ve fulfillment merkezlerindeki stok dagilimini tutar.
/// </summary>
public class ProductWarehouseStock : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string FulfillmentCenter { get; private set; } = "OwnWarehouse";
    public int AvailableQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int InboundQuantity { get; private set; }
    public DateTime LastSyncedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Kullanilabilir + rezerve toplam miktar.
    /// </summary>
    public int TotalQuantity => AvailableQuantity + ReservedQuantity;

    // EF Core icin parametresiz constructor
    private ProductWarehouseStock() { }

    /// <summary>
    /// Factory method — yeni ProductWarehouseStock olusturur.
    /// </summary>
    public static ProductWarehouseStock Create(Guid productId, Guid warehouseId, string fulfillmentCenter)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty.", nameof(productId));
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId cannot be empty.", nameof(warehouseId));
        ArgumentException.ThrowIfNullOrWhiteSpace(fulfillmentCenter, nameof(fulfillmentCenter));

        return new ProductWarehouseStock
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            FulfillmentCenter = fulfillmentCenter,
            AvailableQuantity = 0,
            ReservedQuantity = 0,
            InboundQuantity = 0,
            LastSyncedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Stok miktarlarini gunceller ve son senkronizasyon zamanini kaydeder.
    /// </summary>
    public void UpdateStock(int available, int reserved, int inbound)
    {
        if (available < 0)
            throw new ArgumentException("Available quantity cannot be negative.", nameof(available));
        if (reserved < 0)
            throw new ArgumentException("Reserved quantity cannot be negative.", nameof(reserved));
        if (inbound < 0)
            throw new ArgumentException("Inbound quantity cannot be negative.", nameof(inbound));

        AvailableQuantity = available;
        ReservedQuantity = reserved;
        InboundQuantity = inbound;
        LastSyncedAt = DateTime.UtcNow;
    }

    public override string ToString() =>
        $"[{FulfillmentCenter}] Product={ProductId} Available={AvailableQuantity} Reserved={ReservedQuantity} Inbound={InboundQuantity}";
}
