using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface IExpenseCategoryRepository
{
    Task<ExpenseCategory?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ExpenseCategory>> GetAllAsync(Guid tenantId, bool? isActive = null, CancellationToken ct = default);
    Task<IReadOnlyList<ExpenseCategory>> GetByParentIdAsync(Guid tenantId, Guid? parentId, CancellationToken ct = default);
    Task AddAsync(ExpenseCategory category, CancellationToken ct = default);
    Task UpdateAsync(ExpenseCategory category, CancellationToken ct = default);
}
