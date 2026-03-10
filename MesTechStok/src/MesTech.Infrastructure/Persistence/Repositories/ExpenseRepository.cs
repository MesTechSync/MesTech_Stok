using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _context;

    public ExpenseRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Expense?> GetByIdAsync(Guid id)
        => await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id).ConfigureAwait(false);

    public async Task<IReadOnlyList<Expense>> GetAllAsync(Guid? tenantId = null)
        => await _context.Expenses
            .Where(e => tenantId == null || e.TenantId == tenantId.Value)
            .OrderByDescending(e => e.Date)
            .ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<Expense>> GetByDateRangeAsync(DateTime from, DateTime to, Guid? tenantId = null)
        => await _context.Expenses
            .Where(e => e.Date >= from && e.Date <= to)
            .Where(e => tenantId == null || e.TenantId == tenantId.Value)
            .OrderByDescending(e => e.Date)
            .ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<Expense>> GetByTypeAsync(ExpenseType type, Guid? tenantId = null)
        => await _context.Expenses
            .Where(e => e.ExpenseType == type)
            .Where(e => tenantId == null || e.TenantId == tenantId.Value)
            .OrderByDescending(e => e.Date)
            .ToListAsync().ConfigureAwait(false);

    public async Task AddAsync(Expense expense)
        => await _context.Expenses.AddAsync(expense).ConfigureAwait(false);

    public Task UpdateAsync(Expense expense)
    {
        _context.Expenses.Update(expense);
        return Task.CompletedTask;
    }
}
