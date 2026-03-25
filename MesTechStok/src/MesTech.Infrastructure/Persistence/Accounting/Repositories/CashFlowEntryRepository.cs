using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class CashFlowEntryRepository : ICashFlowEntryRepository
{
    private readonly AppDbContext _context;
    public CashFlowEntryRepository(AppDbContext context) => _context = context;

    public async Task<CashFlowEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.CashFlowEntries.FindAsync([id], ct);

    public async Task<IReadOnlyList<CashFlowEntry>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CashFlowDirection? direction = null, CancellationToken ct = default)
    {
        var q = _context.CashFlowEntries
            .Include(e => e.Counterparty)
            .Where(e => e.TenantId == tenantId && e.EntryDate >= from && e.EntryDate <= to);
        if (direction.HasValue) q = q.Where(e => e.Direction == direction.Value);
        return await q.OrderByDescending(e => e.EntryDate).AsNoTracking().ToListAsync(ct);
    }

    public async Task<decimal> GetTotalByDirectionAsync(Guid tenantId, CashFlowDirection direction, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.CashFlowEntries
            .Where(e => e.TenantId == tenantId && e.Direction == direction && e.EntryDate >= from && e.EntryDate <= to)
            .SumAsync(e => e.Amount, ct);

    public async Task AddAsync(CashFlowEntry entry, CancellationToken ct = default)
        => await _context.CashFlowEntries.AddAsync(entry, ct);
}
