using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Entities;

using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Persistence.Repositories.Crm;

/// <summary>
/// EF Core implementation for CrmContact aggregate persistence.
/// </summary>
public class CrmContactRepository : ICrmContactRepository
{
    private readonly AppDbContext _context;

    public CrmContactRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<CrmContact?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.CrmContacts.FindAsync([id], ct).ConfigureAwait(false);

    public async Task<(IReadOnlyList<CrmContact> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.CrmContacts.Where(c => c.TenantId == tenantId);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        var count = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking().ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, count);
    }

    public async Task<IReadOnlyList<CrmContact>> GetByTenantAsync(
        Guid tenantId, CancellationToken ct = default)
        => await _context.CrmContacts
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.FullName)
            .AsNoTracking().ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddAsync(CrmContact contact, CancellationToken ct = default)
        => await _context.CrmContacts.AddAsync(contact, ct).ConfigureAwait(false);
}
