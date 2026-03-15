using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface IChartOfAccountsRepository
{
    Task<ChartOfAccounts?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ChartOfAccounts?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct = default);
    Task<IReadOnlyList<ChartOfAccounts>> GetAllAsync(Guid tenantId, bool? isActive = null, CancellationToken ct = default);
    Task<IReadOnlyList<ChartOfAccounts>> GetByParentIdAsync(Guid tenantId, Guid? parentId, CancellationToken ct = default);
    Task AddAsync(ChartOfAccounts account, CancellationToken ct = default);
    Task UpdateAsync(ChartOfAccounts account, CancellationToken ct = default);
}
