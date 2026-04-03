using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IIncomeRepository
{
    Task<Income?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Income>> GetAllAsync(Guid? tenantId = null);
    Task<IReadOnlyList<Income>> GetByDateRangeAsync(DateTime from, DateTime to, Guid? tenantId = null);
    Task<IReadOnlyList<Income>> GetByTypeAsync(IncomeType type, Guid? tenantId = null);
    Task<bool> ExistsByOrderIdAsync(Guid tenantId, Guid orderId, CancellationToken ct = default);
    Task AddAsync(Income income);
    Task UpdateAsync(Income income);
}
