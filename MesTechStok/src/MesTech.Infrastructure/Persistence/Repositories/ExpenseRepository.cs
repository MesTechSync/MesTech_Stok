using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _context;

    public ExpenseRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Expense?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Expenses
            .AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Expense>> GetAllAsync(Guid? tenantId = null, CancellationToken ct = default)
        => await _context.Expenses
            .Where(e => tenantId == null || e.TenantId == tenantId.Value)
            .OrderByDescending(e => e.Date)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Expense>> GetByDateRangeAsync(DateTime from, DateTime to, Guid? tenantId = null, CancellationToken ct = default)
        => await _context.Expenses
            .Where(e => e.Date >= from && e.Date <= to)
            .Where(e => tenantId == null || e.TenantId == tenantId.Value)
            .OrderByDescending(e => e.Date)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<Expense>> GetByTypeAsync(ExpenseType type, Guid? tenantId = null, CancellationToken ct = default)
        => await _context.Expenses
            .Where(e => e.ExpenseType == type)
            .Where(e => tenantId == null || e.TenantId == tenantId.Value)
            .OrderByDescending(e => e.Date)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(Expense expense, CancellationToken ct = default)
        => await _context.Expenses.AddAsync(expense, ct).ConfigureAwait(false);

    public Task UpdateAsync(Expense expense, CancellationToken ct = default)
    {
        _context.Expenses.Update(expense);
        return Task.CompletedTask;
    }
}
