using MesTech.Domain.Entities.Crm;

namespace MesTech.Domain.Interfaces;

public interface ICrmContactRepository
{
    Task<CrmContact?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CrmContact>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(CrmContact contact, CancellationToken ct = default);
}
