using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementasyonu — ISupplierFeedRepository.
/// Sprint-D DEV1 — Dalga 8 Dropshipping aktivasyonu.
/// </summary>
public sealed class SupplierFeedRepository(AppDbContext db) : ISupplierFeedRepository
{
    public async Task<SupplierFeed?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.SupplierFeeds
            .AsNoTracking().FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, ct).ConfigureAwait(false);

    public async Task<(IReadOnlyList<SupplierFeed> Items, int Total)> GetPagedAsync(
        Guid tenantId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.SupplierFeeds
            .Where(f => f.TenantId == tenantId && !f.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(f => f.IsActive == isActive.Value);

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

        return ((IReadOnlyList<SupplierFeed>)items, total);
    }

    public async Task<int> GetActiveCountAsync(Guid tenantId, CancellationToken ct = default)
        => await db.SupplierFeeds
            .CountAsync(f => f.TenantId == tenantId && f.IsActive && !f.IsDeleted, ct).ConfigureAwait(false);

    public async Task<DateTime?> GetLastSyncAtAsync(Guid tenantId, CancellationToken ct = default)
        => await db.SupplierFeeds
            .Where(f => f.TenantId == tenantId && !f.IsDeleted && f.LastSyncAt.HasValue)
            .MaxAsync(f => f.LastSyncAt, ct).ConfigureAwait(false);

    public async Task AddAsync(SupplierFeed feed, CancellationToken ct = default)
    {
        await db.SupplierFeeds.AddAsync(feed, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(SupplierFeed feed, CancellationToken ct = default)
    {
        db.SupplierFeeds.Update(feed);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
