using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ErpAccountMappingRepository : IErpAccountMappingRepository
{
    private readonly AppDbContext _db;

    public ErpAccountMappingRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ErpAccountMapping>> GetByTenantAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        return await _db.Set<ErpAccountMapping>()
            .Where(m => m.TenantId == tenantId)
            .OrderBy(m => m.MesTechAccountCode)
            .Take(1000) // G485: pagination guard
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<ErpAccountMapping?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Set<ErpAccountMapping>().FirstOrDefaultAsync(m => m.Id == id, ct).ConfigureAwait(false);
    }

    public async Task<ErpAccountMapping?> FindByMesTechCodeAsync(
        Guid tenantId, string mesTechCode, CancellationToken ct = default)
    {
        return await _db.Set<ErpAccountMapping>()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.MesTechAccountCode == mesTechCode, ct).ConfigureAwait(false);
    }

    public async Task<ErpAccountMapping?> FindByErpCodeAsync(
        Guid tenantId, string erpCode, CancellationToken ct = default)
    {
        return await _db.Set<ErpAccountMapping>()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.ErpAccountCode == erpCode, ct).ConfigureAwait(false);
    }

    public async Task AddAsync(ErpAccountMapping mapping, CancellationToken ct = default)
    {
        await _db.Set<ErpAccountMapping>().AddAsync(mapping, ct).ConfigureAwait(false);
    }

    public void Remove(ErpAccountMapping mapping)
    {
        _db.Set<ErpAccountMapping>().Remove(mapping);
    }
}
