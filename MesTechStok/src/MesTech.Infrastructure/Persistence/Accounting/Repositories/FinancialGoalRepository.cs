using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class FinancialGoalRepository : IFinancialGoalRepository
{
    private readonly AppDbContext _context;
    public FinancialGoalRepository(AppDbContext context) => _context = context;

    public async Task<FinancialGoal?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.FinancialGoals.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<FinancialGoal>> GetActiveAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.FinancialGoals
            .Where(g => g.TenantId == tenantId && !g.IsAchieved && g.EndDate >= DateTime.UtcNow)
            .OrderBy(g => g.EndDate)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(FinancialGoal goal, CancellationToken ct = default)
        => await _context.FinancialGoals.AddAsync(goal, ct);

    public Task UpdateAsync(FinancialGoal goal, CancellationToken ct = default)
    {
        _context.FinancialGoals.Update(goal);
        return Task.CompletedTask;
    }
}
