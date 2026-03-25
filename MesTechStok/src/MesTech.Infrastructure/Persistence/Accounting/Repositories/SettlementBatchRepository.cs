using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class SettlementBatchRepository : ISettlementBatchRepository
{
    private readonly AppDbContext _context;
    public SettlementBatchRepository(AppDbContext context) => _context = context;

    public async Task<SettlementBatch?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.SettlementBatches
            .Include(b => b.Lines)
            .AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<SettlementBatch>> GetByPlatformAsync(Guid tenantId, string platform, CancellationToken ct = default)
        => await _context.SettlementBatches
            .Include(b => b.Lines)
            .Where(b => b.TenantId == tenantId && b.Platform == platform)
            .OrderByDescending(b => b.PeriodEnd)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<SettlementBatch>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.SettlementBatches
            .Include(b => b.Lines)
            .Where(b => b.TenantId == tenantId && b.PeriodStart >= from && b.PeriodEnd <= to)
            .OrderByDescending(b => b.PeriodEnd)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<SettlementBatch>> GetUnmatchedAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.SettlementBatches
            .Include(b => b.Lines)
            .Where(b => b.TenantId == tenantId && b.Status == MesTech.Domain.Accounting.Enums.SettlementStatus.Imported)
            .OrderByDescending(b => b.PeriodEnd)
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(SettlementBatch batch, CancellationToken ct = default)
        => await _context.SettlementBatches.AddAsync(batch, ct);

    public Task UpdateAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        _context.SettlementBatches.Update(batch);
        return Task.CompletedTask;
    }
}
