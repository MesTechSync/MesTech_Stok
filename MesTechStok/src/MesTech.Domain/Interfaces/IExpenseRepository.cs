using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Expense>> GetAllAsync(Guid? tenantId = null);
    Task<IReadOnlyList<Expense>> GetByDateRangeAsync(DateTime from, DateTime to, Guid? tenantId = null);
    Task<IReadOnlyList<Expense>> GetByTypeAsync(ExpenseType type, Guid? tenantId = null);
    Task AddAsync(Expense expense);
    Task UpdateAsync(Expense expense);
}
