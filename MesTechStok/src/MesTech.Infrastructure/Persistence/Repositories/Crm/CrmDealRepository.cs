using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Persistence.Repositories.Crm;

/// <summary>
/// EF Core implementation for Deal aggregate persistence.
/// </summary>
public class CrmDealRepository : ICrmDealRepository
{
    private readonly AppDbContext _context;

    public CrmDealRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Deal?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Deals
            .Include(d => d.Stage)
            .Include(d => d.Contact)
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Deal>> GetByPipelineAsync(
        Guid tenantId, Guid pipelineId, DealStatus? status, CancellationToken ct = default)
    {
        var query = _context.Deals
            .Include(d => d.Stage)
            .Include(d => d.Contact)
            .Where(d => d.TenantId == tenantId && d.PipelineId == pipelineId);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Deal>> GetByContactAsync(Guid contactId, CancellationToken ct = default)
        => await _context.Deals
            .Include(d => d.Stage)
            .Where(d => d.CrmContactId == contactId)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddAsync(Deal deal, CancellationToken ct = default)
        => await _context.Deals.AddAsync(deal, ct).ConfigureAwait(false);

    // DEV3 H28 T3.3 — Tenant bazlı sayfalı deal sorgusu
    public async Task<IReadOnlyList<Deal>> GetByTenantPagedAsync(
        Guid tenantId, DealStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Deals
            .Include(d => d.Stage)
            .Include(d => d.Contact)
            .Where(d => d.TenantId == tenantId);

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
