using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Store>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Store>> GetByPlatformTypeAsync(PlatformType platformType, CancellationToken ct = default);
    Task AddAsync(Store store, CancellationToken ct = default);
    Task UpdateAsync(Store store, CancellationToken ct = default);
    Task DeleteAsync(Store store, CancellationToken ct = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsByTenantAndPlatformAsync(Guid tenantId, PlatformType platformType, string storeName, CancellationToken ct = default);
}
