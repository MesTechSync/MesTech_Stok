namespace MesTech.Application.Interfaces;

/// <summary>
/// Çoklu depo desteği olan platform adaptörleri için opsiyonel interface.
/// </summary>
public interface IMultiWarehouseAdapter
{
    Task<IEnumerable<PlatformWarehouse>> GetWarehousesAsync(CancellationToken ct = default);
    Task<bool> UpdateWarehouseStockAsync(string warehouseId, IEnumerable<StockUpdate> updates, CancellationToken ct = default);
}

public record PlatformWarehouse(string ExternalId, string Name, bool IsActive);
public record StockUpdate(string ExternalProductId, int Quantity);
