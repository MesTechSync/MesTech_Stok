using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IWorkTaskRepository
{
    Task<WorkTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WorkTask>> GetByProjectAsync(Guid projectId, WorkTaskStatus? status, Guid? assignedToUserId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkTask>> GetByUserAsync(Guid userId, Guid tenantId, WorkTaskStatus? status, CancellationToken ct = default);
    Task<IReadOnlyList<WorkTask>> GetOverdueAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(WorkTask task, CancellationToken ct = default);
}
