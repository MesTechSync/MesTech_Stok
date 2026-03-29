using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IStockPlacementRepository
{
    Task<IReadOnlyList<StockPlacement>> GetByTenantAsync(
        Guid tenantId, Guid? warehouseId = null, string? shelfCode = null,
        CancellationToken ct = default);

    Task<StockPlacement?> GetByProductAndLocationAsync(
        Guid tenantId, Guid productId, Guid warehouseId, Guid? shelfId = null,
        CancellationToken ct = default);

    Task AddAsync(StockPlacement placement, CancellationToken ct = default);
}
