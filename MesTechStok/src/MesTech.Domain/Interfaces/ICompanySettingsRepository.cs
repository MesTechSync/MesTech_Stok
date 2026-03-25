using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface ICompanySettingsRepository
{
    Task<CompanySettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(CompanySettings settings, CancellationToken ct = default);
    Task UpdateAsync(CompanySettings settings, CancellationToken ct = default);
}
