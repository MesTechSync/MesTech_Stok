using MesTech.Domain.Entities.Tasks;

namespace MesTech.Domain.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
}
