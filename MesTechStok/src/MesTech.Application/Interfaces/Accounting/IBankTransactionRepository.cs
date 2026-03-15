using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface IBankTransactionRepository
{
    Task<BankTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BankTransaction?> GetByIdempotencyKeyAsync(Guid tenantId, string idempotencyKey, CancellationToken ct = default);
    Task<IReadOnlyList<BankTransaction>> GetByBankAccountAsync(Guid tenantId, Guid bankAccountId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<IReadOnlyList<BankTransaction>> GetUnreconciledAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(BankTransaction transaction, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<BankTransaction> transactions, CancellationToken ct = default);
    Task UpdateAsync(BankTransaction transaction, CancellationToken ct = default);
}
