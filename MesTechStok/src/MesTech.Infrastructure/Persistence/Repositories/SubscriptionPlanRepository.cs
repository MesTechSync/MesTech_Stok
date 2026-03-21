using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly AppDbContext _context;

    public SubscriptionPlanRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<SubscriptionPlan>> GetActiveAsync(CancellationToken ct = default)
        => await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(SubscriptionPlan plan, CancellationToken ct = default)
        => await _context.SubscriptionPlans.AddAsync(plan, ct);

    public Task UpdateAsync(SubscriptionPlan plan, CancellationToken ct = default)
    {
        _context.SubscriptionPlans.Update(plan);
        return Task.CompletedTask;
    }
}
