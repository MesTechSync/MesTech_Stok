using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class JournalEntryRepository : IJournalEntryRepository
{
    private readonly AppDbContext _context;
    public JournalEntryRepository(AppDbContext context) => _context = context;

    public async Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<bool> ExistsByReferenceAsync(Guid tenantId, string referenceNumber, CancellationToken ct = default)
        => await _context.JournalEntries
            .AsNoTracking()
            .AnyAsync(j => j.TenantId == tenantId && j.ReferenceNumber == referenceNumber, ct);

    public async Task<IReadOnlyList<JournalEntry>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.JournalEntries
            .Include(e => e.Lines)
            .Where(e => e.TenantId == tenantId && e.EntryDate >= from && e.EntryDate <= to)
            .OrderByDescending(e => e.EntryDate)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<JournalEntry>> GetByAccountIdAsync(Guid tenantId, Guid accountId, CancellationToken ct = default)
        => await _context.JournalEntries
            .Include(e => e.Lines)
            .Where(e => e.TenantId == tenantId && e.Lines.Any(l => l.AccountId == accountId))
            .OrderByDescending(e => e.EntryDate)
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(JournalEntry entry, CancellationToken ct = default)
        => await _context.JournalEntries.AddAsync(entry, ct);

    public Task UpdateAsync(JournalEntry entry, CancellationToken ct = default)
    {
        _context.JournalEntries.Update(entry);
        return Task.CompletedTask;
    }
}
