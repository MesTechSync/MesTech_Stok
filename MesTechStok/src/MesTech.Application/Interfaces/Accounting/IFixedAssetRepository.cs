using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Sabit kiymet repository arayuzu.
/// </summary>
public interface IFixedAssetRepository
{
    Task<FixedAsset?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FixedAsset>> GetAllAsync(Guid tenantId, bool? isActive = null, CancellationToken ct = default);
    Task AddAsync(FixedAsset asset, CancellationToken ct = default);
}
