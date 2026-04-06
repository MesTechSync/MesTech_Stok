using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly AppDbContext _context;

    public SubscriptionPlanRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<SubscriptionPlan>> GetActiveAsync(CancellationToken ct = default)
        => await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(SubscriptionPlan plan, CancellationToken ct = default)
        => await _context.SubscriptionPlans.AddAsync(plan, ct).ConfigureAwait(false);

    public Task UpdateAsync(SubscriptionPlan plan, CancellationToken ct = default)
    {
        _context.SubscriptionPlans.Update(plan);
        return Task.CompletedTask;
    }
}
