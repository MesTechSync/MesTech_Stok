using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class JournalEntryRepository : IJournalEntryRepository
{
    private readonly AppDbContext _context;

    public JournalEntryRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<bool> ExistsByReferenceAsync(Guid tenantId, string referenceNumber, CancellationToken ct = default)
        => await _context.JournalEntries
            .AsNoTracking()
            .AnyAsync(j => j.TenantId == tenantId && j.ReferenceNumber == referenceNumber, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<JournalEntry>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.TenantId == tenantId && j.EntryDate >= from && j.EntryDate <= to)
            .OrderByDescending(j => j.EntryDate)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<JournalEntry>> GetByAccountIdAsync(Guid tenantId, Guid accountId, CancellationToken ct = default)
        => await _context.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.TenantId == tenantId && j.Lines.Any(l => l.AccountId == accountId))
            .OrderByDescending(j => j.EntryDate)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(JournalEntry entry, CancellationToken ct = default)
    {
        await _context.JournalEntries.AddAsync(entry, ct).ConfigureAwait(false);
    }

    public Task UpdateAsync(JournalEntry entry, CancellationToken ct = default)
    {
        _context.JournalEntries.Update(entry);
        return Task.CompletedTask;
    }
}
