using MesTech.Domain.Accounting.Entities;

namespace MesTech.Domain.Interfaces;

public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByReferenceAsync(Guid tenantId, string referenceNumber, CancellationToken ct = default);
    Task AddAsync(JournalEntry entry, CancellationToken ct = default);
    Task UpdateAsync(JournalEntry entry, CancellationToken ct = default);
}
