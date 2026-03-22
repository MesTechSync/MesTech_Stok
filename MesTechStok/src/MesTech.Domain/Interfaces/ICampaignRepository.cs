using MesTech.Domain.Entities.Crm;

namespace MesTech.Domain.Interfaces;

public interface ICampaignRepository
{
    Task<Campaign?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Campaign>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Campaign>> GetActiveByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task AddAsync(Campaign campaign, CancellationToken ct = default);
}
