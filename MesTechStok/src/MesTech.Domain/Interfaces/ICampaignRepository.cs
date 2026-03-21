using MesTech.Domain.Entities.Crm;

namespace MesTech.Domain.Interfaces;

public interface ICampaignRepository
{
    Task<Campaign?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Campaign>> GetByTenantAsync(Guid tenantId, bool? activeOnly = null, CancellationToken ct = default);
    Task AddAsync(Campaign campaign, CancellationToken ct = default);
    Task UpdateAsync(Campaign campaign, CancellationToken ct = default);
}
