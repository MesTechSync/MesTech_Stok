using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Persistence.Repositories.Tasks;

public sealed class WorkTaskRepository : IWorkTaskRepository
{
    private readonly AppDbContext _context;

    public WorkTaskRepository(AppDbContext context) => _context = context;

    public async Task<WorkTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.WorkTasks
            .AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<WorkTask>> GetByProjectAsync(
        Guid projectId, WorkTaskStatus? status, Guid? assignedToUserId, CancellationToken ct = default)
    {
        var q = _context.WorkTasks.Where(t => t.ProjectId == projectId);
        if (status.HasValue)
            q = q.Where(t => t.Status == status.Value);
        if (assignedToUserId.HasValue)
            q = q.Where(t => t.AssignedToUserId == assignedToUserId.Value);
        return await q
            .OrderBy(t => t.Position)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WorkTask>> GetByUserAsync(
        Guid userId, Guid tenantId, WorkTaskStatus? status, CancellationToken ct = default)
    {
        var q = _context.WorkTasks
            .Where(t => t.TenantId == tenantId && t.AssignedToUserId == userId);
        if (status.HasValue)
            q = q.Where(t => t.Status == status.Value);
        return await q
            .OrderBy(t => t.DueDate)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WorkTask>> GetOverdueAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.WorkTasks
            .Where(t => t.TenantId == tenantId
                     && t.DueDate < DateTime.UtcNow
                     && t.Status != WorkTaskStatus.Done
                     && t.Status != WorkTaskStatus.Cancelled)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddAsync(WorkTask task, CancellationToken ct = default)
        => await _context.WorkTasks.AddAsync(task, ct).ConfigureAwait(false);
}
