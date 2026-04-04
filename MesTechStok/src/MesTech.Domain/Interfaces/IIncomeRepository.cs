using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IIncomeRepository
{
    Task<Income?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Income>> GetAllAsync(Guid? tenantId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Income>> GetByDateRangeAsync(DateTime from, DateTime to, Guid? tenantId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Income>> GetByTypeAsync(IncomeType type, Guid? tenantId = null, CancellationToken ct = default);
    Task<bool> ExistsByOrderIdAsync(Guid tenantId, Guid orderId, CancellationToken ct = default);
    Task AddAsync(Income income, CancellationToken ct = default);
    Task UpdateAsync(Income income, CancellationToken ct = default);
}
