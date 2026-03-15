using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public class BankTransactionRepository : IBankTransactionRepository
{
    private readonly AppDbContext _context;
    public BankTransactionRepository(AppDbContext context) => _context = context;

    public async Task<BankTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.AccountingBankTransactions.FindAsync([id], ct);

    public async Task<BankTransaction?> GetByIdempotencyKeyAsync(Guid tenantId, string idempotencyKey, CancellationToken ct = default)
        => await _context.AccountingBankTransactions
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.IdempotencyKey == idempotencyKey, ct);

    public async Task<IReadOnlyList<BankTransaction>> GetByBankAccountAsync(Guid tenantId, Guid bankAccountId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var q = _context.AccountingBankTransactions
            .Where(t => t.TenantId == tenantId && t.BankAccountId == bankAccountId);
        if (from.HasValue) q = q.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) q = q.Where(t => t.TransactionDate <= to.Value);
        return await q.OrderByDescending(t => t.TransactionDate).AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BankTransaction>> GetUnreconciledAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.AccountingBankTransactions
            .Where(t => t.TenantId == tenantId && !t.IsReconciled)
            .OrderByDescending(t => t.TransactionDate)
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(BankTransaction transaction, CancellationToken ct = default)
        => await _context.AccountingBankTransactions.AddAsync(transaction, ct);

    public async Task AddRangeAsync(IEnumerable<BankTransaction> transactions, CancellationToken ct = default)
        => await _context.AccountingBankTransactions.AddRangeAsync(transactions, ct);

    public Task UpdateAsync(BankTransaction transaction, CancellationToken ct = default)
    {
        _context.AccountingBankTransactions.Update(transaction);
        return Task.CompletedTask;
    }
}
