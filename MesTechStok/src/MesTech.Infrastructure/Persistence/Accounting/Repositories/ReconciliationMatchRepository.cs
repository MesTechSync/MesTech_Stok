using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public class ReconciliationMatchRepository : IReconciliationMatchRepository
{
    private readonly AppDbContext _context;
    public ReconciliationMatchRepository(AppDbContext context) => _context = context;

    public async Task<ReconciliationMatch?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ReconciliationMatches.FindAsync([id], ct);

    public async Task<IReadOnlyList<ReconciliationMatch>> GetByStatusAsync(Guid tenantId, ReconciliationStatus status, CancellationToken ct = default)
        => await _context.ReconciliationMatches
            .Where(m => m.TenantId == tenantId && m.Status == status)
            .OrderByDescending(m => m.MatchDate)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<ReconciliationMatch>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.ReconciliationMatches
            .Where(m => m.TenantId == tenantId && m.MatchDate >= from && m.MatchDate <= to)
            .OrderByDescending(m => m.MatchDate)
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(ReconciliationMatch match, CancellationToken ct = default)
        => await _context.ReconciliationMatches.AddAsync(match, ct);

    public Task UpdateAsync(ReconciliationMatch match, CancellationToken ct = default)
    {
        _context.ReconciliationMatches.Update(match);
        return Task.CompletedTask;
    }
}
