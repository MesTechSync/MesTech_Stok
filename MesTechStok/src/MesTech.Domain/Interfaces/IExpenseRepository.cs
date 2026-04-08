using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Expense>> GetAllAsync(Guid? tenantId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Expense>> GetByDateRangeAsync(DateTime from, DateTime to, Guid? tenantId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Expense>> GetByTypeAsync(ExpenseType type, Guid? tenantId = null, CancellationToken ct = default);
    Task AddAsync(Expense expense, CancellationToken ct = default);
    Task UpdateAsync(Expense expense, CancellationToken ct = default);
}
