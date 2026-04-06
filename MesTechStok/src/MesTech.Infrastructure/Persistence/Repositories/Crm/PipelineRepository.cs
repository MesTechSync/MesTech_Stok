using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Infrastructure.Persistence.Repositories.Crm;

public sealed class PipelineRepository : IPipelineRepository
{
    private readonly AppDbContext _context;
    public PipelineRepository(AppDbContext context) => _context = context;

    public async Task<Pipeline?> GetByIdWithStagesAsync(Guid id, CancellationToken ct = default)
        => await _context.Pipelines.Include(p => p.Stages.OrderBy(s => s.Position))
                         .AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);

    public async Task<Pipeline?> GetDefaultAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Pipelines.Include(p => p.Stages.OrderBy(s => s.Position))
                         .AsNoTracking().FirstOrDefaultAsync(p => p.TenantId == tenantId && p.IsDefault, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Pipeline>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Pipelines.Where(p => p.TenantId == tenantId).Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(Pipeline pipeline, CancellationToken ct = default)
        => await _context.Pipelines.AddAsync(pipeline, ct).ConfigureAwait(false);
}
