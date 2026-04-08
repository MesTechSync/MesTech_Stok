using MesTech.Application.Interfaces.Reporting;
using MesTech.Domain.Entities.Reporting;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class KpiSnapshotRepository : IKpiSnapshotRepository
{
    private readonly AppDbContext _context;
    public KpiSnapshotRepository(AppDbContext context) => _context = context;

    public async Task<KpiSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.KpiSnapshots.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);

    public async Task<KpiSnapshot?> GetLatestByTypeAsync(Guid tenantId, KpiType type, CancellationToken ct = default)
        => await _context.KpiSnapshots
            .Where(s => s.TenantId == tenantId && s.Type == type)
            .OrderByDescending(s => s.SnapshotDate)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<KpiSnapshot>> GetByDateRangeAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.KpiSnapshots
            .Where(s => s.TenantId == tenantId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .Take(1000)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(KpiSnapshot snapshot, CancellationToken ct = default)
        => await _context.KpiSnapshots.AddAsync(snapshot, ct).ConfigureAwait(false);
}
