using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Persistence.Repositories.Finance;

public sealed class ExpenseRepository : IFinanceExpenseRepository
{
    private readonly AppDbContext _context;
    public ExpenseRepository(AppDbContext context) => _context = context;

    public async Task<FinanceExpense?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.FinanceExpenses.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<FinanceExpense>> GetByTenantAsync(
        Guid tenantId, ExpenseStatus? status, CancellationToken ct = default)
    {
        var q = _context.FinanceExpenses.Where(e => e.TenantId == tenantId);
        if (status.HasValue) q = q.Where(e => e.Status == status.Value);
        return await q.OrderByDescending(e => e.ExpenseDate).AsNoTracking().ToListAsync(ct);
    }

    public async Task<decimal> GetTotalByDateRangeAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.FinanceExpenses
            .Where(e => e.TenantId == tenantId && e.ExpenseDate >= from && e.ExpenseDate <= to
                     && e.Status != ExpenseStatus.Rejected && e.Status != ExpenseStatus.Draft)
            .SumAsync(e => e.Amount, ct);

    public async Task AddAsync(FinanceExpense expense, CancellationToken ct = default)
        => await _context.FinanceExpenses.AddAsync(expense, ct);
}
