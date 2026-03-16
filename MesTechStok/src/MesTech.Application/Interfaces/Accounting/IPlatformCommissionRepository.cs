using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces.Accounting;

public interface IPlatformCommissionRepository
{
    Task<PlatformCommission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PlatformCommission>> GetByPlatformAsync(Guid tenantId, PlatformType? platform = null, bool? isActive = null, CancellationToken ct = default);
    Task AddAsync(PlatformCommission commission, CancellationToken ct = default);
    Task UpdateAsync(PlatformCommission commission, CancellationToken ct = default);
}
