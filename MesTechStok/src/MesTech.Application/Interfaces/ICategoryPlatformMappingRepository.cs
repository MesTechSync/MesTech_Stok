using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

/// <summary>
/// CategoryPlatformMapping veri erisim arayuzu.
/// </summary>
public interface ICategoryPlatformMappingRepository
{
    Task<CategoryPlatformMapping?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryPlatformMapping>> GetByTenantAsync(
        Guid tenantId, PlatformType? platform = null, CancellationToken ct = default);

    Task<CategoryPlatformMapping?> GetByCategoryAndPlatformAsync(
        Guid tenantId, Guid categoryId, PlatformType platform, CancellationToken ct = default);

    Task AddAsync(CategoryPlatformMapping mapping, CancellationToken ct = default);
    Task UpdateAsync(CategoryPlatformMapping mapping, CancellationToken ct = default);
}
