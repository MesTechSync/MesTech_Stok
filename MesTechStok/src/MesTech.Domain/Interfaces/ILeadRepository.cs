using MesTech.Domain.Entities.Crm;

namespace MesTech.Domain.Interfaces;

public interface ILeadRepository
{
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Lead>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> AnyByTenantAndNameAsync(Guid tenantId, string name, CancellationToken ct = default);
    Task AddAsync(Lead lead, CancellationToken ct = default);
    Task UpdateAsync(Lead lead, CancellationToken ct = default);
}
