using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Persistence.Repositories.Tasks;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context) => _context = context;

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Projects
            .AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Project>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Projects
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .AsNoTracking().ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddAsync(Project project, CancellationToken ct = default)
        => await _context.Projects.AddAsync(project, ct).ConfigureAwait(false);
}
