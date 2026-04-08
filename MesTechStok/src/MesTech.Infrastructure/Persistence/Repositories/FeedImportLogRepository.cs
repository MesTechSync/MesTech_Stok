using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementasyonu — IFeedImportLogRepository.
/// Sprint-D DEV1 — Dalga 8 Dropshipping aktivasyonu.
/// </summary>
public sealed class FeedImportLogRepository(AppDbContext db) : IFeedImportLogRepository
{
    public async Task<(IReadOnlyList<FeedImportLog> Items, int Total)> GetByFeedIdPagedAsync(
        Guid feedId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.FeedImportLogs
            .Where(l => l.SupplierFeedId == feedId && !l.IsDeleted)
            .OrderByDescending(l => l.StartedAt);

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

        return ((IReadOnlyList<FeedImportLog>)items, total);
    }

    public async Task AddAsync(FeedImportLog log, CancellationToken ct = default)
    {
        await db.FeedImportLogs.AddAsync(log, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(FeedImportLog log, CancellationToken ct = default)
    {
        db.FeedImportLogs.Update(log);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
