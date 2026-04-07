using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Erp;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ErpFieldMappingRepository : IErpFieldMappingRepository
{
    private readonly AppDbContext _context;
    public ErpFieldMappingRepository(AppDbContext context) => _context = context;

    public async Task<ErpFieldMapping?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ErpFieldMappings.FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<ErpFieldMapping>> GetByErpTypeAsync(Guid tenantId, string erpType, CancellationToken ct = default)
        => await _context.ErpFieldMappings
            .Where(x => x.TenantId == tenantId && x.ErpType == erpType)
            .OrderBy(x => x.MesTechField)
            .Take(500)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(ErpFieldMapping mapping, CancellationToken ct = default)
        => await _context.ErpFieldMappings.AddAsync(mapping, ct).ConfigureAwait(false);

    public Task UpdateAsync(ErpFieldMapping mapping, CancellationToken ct = default)
    {
        _context.ErpFieldMappings.Update(mapping);
        return Task.CompletedTask;
    }
}
