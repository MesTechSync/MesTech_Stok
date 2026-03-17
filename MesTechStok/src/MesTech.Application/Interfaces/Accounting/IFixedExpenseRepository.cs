using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface IFixedExpenseRepository
{
    Task<FixedExpense?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FixedExpense>> GetAllAsync(Guid tenantId, bool? isActive = null, CancellationToken ct = default);
    Task AddAsync(FixedExpense expense, CancellationToken ct = default);
    Task UpdateAsync(FixedExpense expense, CancellationToken ct = default);
}
