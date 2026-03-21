using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;

using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Persistence.Repositories.Crm;

/// <summary>
/// EF Core implementation for Lead aggregate persistence.
/// DEV1-DEPENDENCY: Implements ICrmLeadRepository once DEV 1 creates it in Domain.Interfaces.
/// </summary>
public class CrmLeadRepository : ICrmLeadRepository
{
    private readonly AppDbContext _context;

    public CrmLeadRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Leads.FindAsync([id], ct).ConfigureAwait(false);

    public async Task<(IReadOnlyList<Lead> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId, LeadStatus? status, Guid? assignedToUserId,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Leads.Where(l => l.TenantId == tenantId);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);
        if (assignedToUserId.HasValue)
            query = query.Where(l => l.AssignedToUserId == assignedToUserId.Value);

        var count = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .AsNoTracking().ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, count);
    }

    public async Task AddAsync(Lead lead, CancellationToken ct = default)
        => await _context.Leads.AddAsync(lead, ct).ConfigureAwait(false);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.Leads.AnyAsync(l => l.Id == id, ct).ConfigureAwait(false);
}
