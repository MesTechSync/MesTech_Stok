using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Persistence.Repositories.Crm;

/// <summary>
/// EF Core implementation for Activity aggregate persistence.
/// </summary>
public sealed class ActivityRepository : IActivityRepository
{
    private readonly AppDbContext _context;

    public ActivityRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Activity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Activities.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Activity>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Activities
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .OrderByDescending(a => a.OccurredAt)
            .AsNoTracking().ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Activity>> GetByContactAsync(Guid contactId, CancellationToken ct = default)
        => await _context.Activities
            .Where(a => a.CrmContactId == contactId && !a.IsDeleted)
            .OrderByDescending(a => a.OccurredAt)
            .AsNoTracking().ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddAsync(Activity activity, CancellationToken ct = default)
        => await _context.Activities.AddAsync(activity, ct).ConfigureAwait(false);
}
