using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Interfaces.Accounting;

public interface IPersonalExpenseRepository
{
    Task<PersonalExpense?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PersonalExpense>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, ExpenseSource? source = null, CancellationToken ct = default);
    Task<decimal> GetTotalByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(PersonalExpense expense, CancellationToken ct = default);
    Task UpdateAsync(PersonalExpense expense, CancellationToken ct = default);
}
