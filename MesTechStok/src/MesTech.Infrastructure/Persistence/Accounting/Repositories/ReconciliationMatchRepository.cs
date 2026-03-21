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

    public async Task<(IReadOnlyList<ReconciliationMatch> Items, int TotalCount)> GetPendingReviewsPagedAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.ReconciliationMatches
            .Where(m => m.TenantId == tenantId && m.Status == ReconciliationStatus.NeedsReview)
            .OrderByDescending(m => m.Confidence);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .AsNoTracking().ToListAsync(ct);

        return (items.AsReadOnly(), totalCount);
    }

    public async Task AddAsync(ReconciliationMatch match, CancellationToken ct = default)
        => await _context.ReconciliationMatches.AddAsync(match, ct);

    public Task UpdateAsync(ReconciliationMatch match, CancellationToken ct = default)
    {
        _context.ReconciliationMatches.Update(match);
        return Task.CompletedTask;
    }
}
