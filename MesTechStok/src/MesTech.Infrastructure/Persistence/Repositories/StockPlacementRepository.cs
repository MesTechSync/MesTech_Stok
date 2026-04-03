using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class StockPlacementRepository : IStockPlacementRepository
{
    private readonly AppDbContext _db;

    public StockPlacementRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<StockPlacement>> GetByTenantAsync(
        Guid tenantId, Guid? warehouseId = null, string? shelfCode = null,
        CancellationToken ct = default)
    {
        var query = _db.Set<StockPlacement>()
            .Where(p => p.TenantId == tenantId);

        if (warehouseId.HasValue)
            query = query.Where(p => p.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(shelfCode))
            query = query.Where(p => p.ShelfCode == shelfCode);

        return await query
            .OrderBy(p => p.WarehouseName)
            .ThenBy(p => p.ShelfCode)
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<StockPlacement?> GetByProductAndLocationAsync(
        Guid tenantId, Guid productId, Guid warehouseId, Guid? shelfId = null,
        CancellationToken ct = default)
    {
        return await _db.Set<StockPlacement>()
            .FirstOrDefaultAsync(p =>
                p.TenantId == tenantId &&
                p.ProductId == productId &&
                p.WarehouseId == warehouseId &&
                p.ShelfId == shelfId, ct).ConfigureAwait(false);
    }

    public async Task AddAsync(StockPlacement placement, CancellationToken ct = default)
    {
        await _db.Set<StockPlacement>().AddAsync(placement, ct).ConfigureAwait(false);
    }
}
