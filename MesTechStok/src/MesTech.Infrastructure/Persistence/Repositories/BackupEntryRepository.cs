using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class BackupEntryRepository : IBackupEntryRepository
{
    private readonly AppDbContext _db;

    public BackupEntryRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<BackupEntry>> GetByTenantAsync(
        Guid tenantId, int limit = 20, CancellationToken ct = default)
    {
        return await _db.Set<BackupEntry>()
            .Where(b => b.TenantId == tenantId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddAsync(BackupEntry entry, CancellationToken ct = default)
    {
        await _db.Set<BackupEntry>().AddAsync(entry, ct);
    }
}
