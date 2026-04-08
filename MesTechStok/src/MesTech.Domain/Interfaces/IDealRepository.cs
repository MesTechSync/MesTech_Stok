using MesTech.Domain.Entities.Crm;

namespace MesTech.Domain.Interfaces;

public interface IDealRepository
{
    Task<Deal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Deal?> GetByIdTrackedWithContactAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Deal>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Deal deal, CancellationToken ct = default);
    Task UpdateAsync(Deal deal, CancellationToken ct = default);
}
