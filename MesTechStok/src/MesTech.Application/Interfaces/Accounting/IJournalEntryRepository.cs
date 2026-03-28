using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Application-layer IJournalEntryRepository.
/// Domain.Interfaces.IJournalEntryRepository ile ayni kontrat.
/// 70 Accounting handler bu namespace'ten referans aliyor.
/// </summary>
public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByReferenceAsync(Guid tenantId, string referenceNumber, CancellationToken ct = default);
    Task<IReadOnlyList<JournalEntry>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<JournalEntry>> GetByAccountIdAsync(Guid tenantId, Guid accountId, CancellationToken ct = default);
    Task AddAsync(JournalEntry entry, CancellationToken ct = default);
    Task UpdateAsync(JournalEntry entry, CancellationToken ct = default);
}
