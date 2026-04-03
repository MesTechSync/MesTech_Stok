using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class TenantSubscriptionRepository : ITenantSubscriptionRepository
{
    private readonly AppDbContext _context;

    public TenantSubscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TenantSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);

    public async Task<TenantSubscription?> GetActiveByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId &&
                        (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
            .OrderByDescending(s => s.CreatedAt)
            .AsNoTracking().FirstOrDefaultAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<TenantSubscription>> GetExpiringAsync(int withinDays, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(withinDays);
        return await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Active &&
                        s.NextBillingDate.HasValue &&
                        s.NextBillingDate.Value <= cutoff)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TenantSubscription>> GetDueForRenewalAsync(DateTime asOfDate, CancellationToken ct = default)
        => await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Active &&
                        s.NextBillingDate.HasValue &&
                        s.NextBillingDate.Value <= asOfDate)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<TenantSubscription>> GetByStatusAsync(SubscriptionStatus status, CancellationToken ct = default)
        => await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.Status == status)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(TenantSubscription subscription, CancellationToken ct = default)
        => await _context.TenantSubscriptions.AddAsync(subscription, ct).ConfigureAwait(false);

    public Task UpdateAsync(TenantSubscription subscription, CancellationToken ct = default)
    {
        _context.TenantSubscriptions.Update(subscription);
        return Task.CompletedTask;
    }
}
