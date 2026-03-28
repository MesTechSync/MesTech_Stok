using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IImportTemplateRepository
{
    Task<IReadOnlyList<ImportTemplate>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<ImportTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ImportTemplate template, CancellationToken ct = default);
    Task UpdateAsync(ImportTemplate template, CancellationToken ct = default);
    Task DeleteAsync(ImportTemplate template, CancellationToken ct = default);
}
