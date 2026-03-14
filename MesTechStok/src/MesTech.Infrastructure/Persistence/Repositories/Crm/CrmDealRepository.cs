// DEV1-DEPENDENCY: Deal entity must be created in MesTech.Domain.Entities.Crm
// DEV1-DEPENDENCY: ICrmDealRepository interface must be added to MesTech.Domain.Interfaces
// This file is a stub — uncomment body when DEV 1 creates Deal entity.

// using Microsoft.EntityFrameworkCore;
// using MesTech.Domain.Entities.Crm;
// using MesTech.Domain.Enums;
// using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Persistence.Repositories.Crm;

/// <summary>
/// EF Core implementation for Deal aggregate persistence.
/// DEV1-DEPENDENCY: Requires Deal entity + ICrmDealRepository interface.
///
/// When DEV 1 provides the entity, restore this implementation:
/// <code>
/// public class CrmDealRepository // : ICrmDealRepository
/// {
///     private readonly AppDbContext _context;
///
///     public CrmDealRepository(AppDbContext context) => _context = context;
///
///     public async Task&lt;Deal?&gt; GetByIdAsync(Guid id, CancellationToken ct = default)
///         => await _context.Deals
///             .Include(d => d.Stage)
///             .Include(d => d.Contact)
///             .FirstOrDefaultAsync(d => d.Id == id, ct);
///
///     public async Task&lt;(IReadOnlyList&lt;Deal&gt; Items, int TotalCount)&gt; GetPagedAsync(
///         Guid tenantId, Guid? contactId, DealStatus? status,
///         int page, int pageSize, CancellationToken ct = default)
///     {
///         var query = _context.Deals.Where(d => d.TenantId == tenantId);
///         if (contactId.HasValue) query = query.Where(d => d.CrmContactId == contactId.Value);
///         if (status.HasValue) query = query.Where(d => d.Status == status.Value);
///         var count = await query.CountAsync(ct);
///         var items = await query.OrderByDescending(d => d.CreatedAt)
///             .Skip((page - 1) * pageSize).Take(pageSize).AsNoTracking().ToListAsync(ct);
///         return (items, count);
///     }
///
///     public async Task AddAsync(Deal deal, CancellationToken ct = default)
///         => await _context.Deals.AddAsync(deal, ct);
/// }
/// </code>
/// </summary>
public static class CrmDealRepositoryStub
{
    // Placeholder class so the file is valid C# until DEV 1 entities arrive.
}
