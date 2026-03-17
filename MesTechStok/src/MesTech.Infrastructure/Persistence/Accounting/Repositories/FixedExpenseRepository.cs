using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public class FixedExpenseRepository : IFixedExpenseRepository
{
    private readonly AppDbContext _context;
    public FixedExpenseRepository(AppDbContext context) => _context = context;

    public async Task<FixedExpense?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.FixedExpenses.FindAsync([id], ct);

    public async Task<IReadOnlyList<FixedExpense>> GetAllAsync(Guid tenantId, bool? isActive = null, CancellationToken ct = default)
    {
        var query = _context.FixedExpenses.Where(e => e.TenantId == tenantId);

        if (isActive.HasValue)
            query = query.Where(e => e.IsActive == isActive.Value);

        return await query
            .OrderBy(e => e.Name)
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task AddAsync(FixedExpense expense, CancellationToken ct = default)
        => await _context.FixedExpenses.AddAsync(expense, ct);

    public Task UpdateAsync(FixedExpense expense, CancellationToken ct = default)
    {
        _context.FixedExpenses.Update(expense);
        return Task.CompletedTask;
    }
}
