using MesTech.Domain.Entities.Crm;

namespace MesTech.Domain.Interfaces;

public interface IPipelineRepository
{
    Task<Pipeline?> GetByIdWithStagesAsync(Guid id, CancellationToken ct = default);
    Task<Pipeline?> GetDefaultAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Pipeline>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Pipeline pipeline, CancellationToken ct = default);
}
