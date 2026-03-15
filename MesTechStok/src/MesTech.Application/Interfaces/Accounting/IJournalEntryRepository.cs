using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<JournalEntry>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<JournalEntry>> GetByAccountIdAsync(Guid tenantId, Guid accountId, CancellationToken ct = default);
    Task AddAsync(JournalEntry entry, CancellationToken ct = default);
}
