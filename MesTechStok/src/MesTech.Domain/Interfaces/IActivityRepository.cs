using MesTech.Domain.Entities.Crm;

namespace MesTech.Domain.Interfaces;

public interface IActivityRepository
{
    Task<Activity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Activity>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Activity>> GetByContactAsync(Guid contactId, CancellationToken ct = default);
    Task AddAsync(Activity activity, CancellationToken ct = default);
}
