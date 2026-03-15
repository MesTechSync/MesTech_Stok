using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface IFinancialGoalRepository
{
    Task<FinancialGoal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FinancialGoal>> GetActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(FinancialGoal goal, CancellationToken ct = default);
    Task UpdateAsync(FinancialGoal goal, CancellationToken ct = default);
}
