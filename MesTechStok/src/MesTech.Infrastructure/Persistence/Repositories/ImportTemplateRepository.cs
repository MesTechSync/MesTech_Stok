using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ImportTemplateRepository : IImportTemplateRepository
{
    private readonly AppDbContext _db;

    public ImportTemplateRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ImportTemplate>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.Set<ImportTemplate>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .Include(x => x.Mappings)
            .OrderByDescending(x => x.LastUsedAt)
            .Take(1000) // G485: pagination guard
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<ImportTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Set<ImportTemplate>()
            .Include(x => x.Mappings)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct).ConfigureAwait(false);

    public async Task AddAsync(ImportTemplate template, CancellationToken ct = default)
        => await _db.Set<ImportTemplate>().AddAsync(template, ct).ConfigureAwait(false);

    public Task UpdateAsync(ImportTemplate template, CancellationToken ct = default)
    {
        _db.Set<ImportTemplate>().Update(template);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ImportTemplate template, CancellationToken ct = default)
    {
        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}
