using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IFinanceExpenseRepository
{
    Task<FinanceExpense?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FinanceExpense>> GetByTenantAsync(Guid tenantId, ExpenseStatus? status, CancellationToken ct = default);
    Task<decimal> GetTotalByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(FinanceExpense expense, CancellationToken ct = default);
}
