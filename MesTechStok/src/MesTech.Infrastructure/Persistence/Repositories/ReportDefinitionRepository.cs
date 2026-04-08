using MesTech.Application.Interfaces.Reporting;
using MesTech.Domain.Entities.Reporting;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ReportDefinitionRepository : IReportDefinitionRepository
{
    private readonly AppDbContext _context;
    public ReportDefinitionRepository(AppDbContext context) => _context = context;

    public async Task<ReportDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ReportDefinitions.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<ReportDefinition>> GetByTenantAsync(Guid tenantId, ReportType? type = null, CancellationToken ct = default)
    {
        var q = _context.ReportDefinitions.Where(x => x.TenantId == tenantId);
        if (type.HasValue) q = q.Where(x => x.Type == type.Value);
        return await q.OrderBy(x => x.Name).Take(500).AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(ReportDefinition definition, CancellationToken ct = default)
        => await _context.ReportDefinitions.AddAsync(definition, ct).ConfigureAwait(false);

    public Task UpdateAsync(ReportDefinition definition, CancellationToken ct = default)
    {
        _context.ReportDefinitions.Update(definition);
        return Task.CompletedTask;
    }
}
