using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementasyonu — IDropshippingPoolRepository.
/// Sprint-B DEV1 B-06
/// </summary>
public sealed class DropshippingPoolRepository(AppDbContext db) : IDropshippingPoolRepository
{
    public async Task<DropshippingPool?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.DropshippingPools
            .Include(p => p.Products)
            .AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

    public async Task<(IReadOnlyList<DropshippingPool> Items, int Total)> GetPoolsPagedAsync(
        Guid tenantId, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.DropshippingPools
            .Where(p => p.TenantId == tenantId && !p.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking().ToListAsync(ct);

        return ((IReadOnlyList<DropshippingPool>)items, total);
    }

    public async Task<(IReadOnlyList<DropshippingPoolProduct> Items, int Total)> GetProductsPagedAsync(
        Guid tenantId, Guid? poolId, ReliabilityColor? colorFilter,
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.DropshippingPoolProducts
            .Include(p => p.Product)
            .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.IsActive);

        if (poolId.HasValue)
            query = query.Where(p => p.PoolId == poolId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p =>
                (p.Product != null && p.Product.Name.ToLower().Contains(s)) ||
                (p.Product != null && p.Product.SKU.ToLower().Contains(s)));
        }

        // colorFilter: ReliabilityColor entity'de henüz yok — ileriki sprintlerde
        // entity genişletilince bu filtre aktif edilecek. Şimdilik ignore.

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking().ToListAsync(ct);

        return ((IReadOnlyList<DropshippingPoolProduct>)items, total);
    }

    public async Task<DropshippingPoolProduct?> GetPoolProductByIdAsync(
        Guid poolProductId, CancellationToken ct = default)
        => await db.DropshippingPoolProducts
            .Include(p => p.Product)
            .AsNoTracking().FirstOrDefaultAsync(p => p.Id == poolProductId && !p.IsDeleted, ct);

    public async Task<IReadOnlyList<DropshippingPoolProduct>> GetPoolProductsByIdsAsync(
        Guid poolId, IEnumerable<Guid> productIds, CancellationToken ct = default)
    {
        var ids = productIds.ToList();
        return await db.DropshippingPoolProducts
            .Include(p => p.Product)
            .Where(p => p.PoolId == poolId && ids.Contains(p.Id) && !p.IsDeleted)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task<PoolStats> GetStatsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var total = await db.DropshippingPoolProducts
            .CountAsync(p => p.TenantId == tenantId && !p.IsDeleted && p.IsActive, ct);

        // ReliabilityColor breakdown: entity'de henüz yok — placeholder
        return new PoolStats(Total: total, Green: 0, Yellow: 0, Orange: 0, Red: 0, AverageScore: 0);
    }

    public async Task AddAsync(DropshippingPool pool, CancellationToken ct = default)
    {
        await db.DropshippingPools.AddAsync(pool, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DropshippingPool pool, CancellationToken ct = default)
    {
        db.DropshippingPools.Update(pool);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddPoolProductAsync(DropshippingPoolProduct poolProduct, CancellationToken ct = default)
    {
        await db.DropshippingPoolProducts.AddAsync(poolProduct, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdatePoolProductAsync(
        DropshippingPoolProduct product, CancellationToken ct = default)
    {
        db.DropshippingPoolProducts.Update(product);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemovePoolProductAsync(
        DropshippingPoolProduct product, CancellationToken ct = default)
    {
        db.DropshippingPoolProducts.Remove(product);
        await db.SaveChangesAsync(ct);
    }
}
