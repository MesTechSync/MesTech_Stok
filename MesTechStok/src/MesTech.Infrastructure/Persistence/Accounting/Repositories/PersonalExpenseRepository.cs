using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class PersonalExpenseRepository : IPersonalExpenseRepository
{
    private readonly AppDbContext _context;
    public PersonalExpenseRepository(AppDbContext context) => _context = context;

    public async Task<PersonalExpense?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.PersonalExpenses.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<PersonalExpense>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, ExpenseSource? source = null, CancellationToken ct = default)
    {
        var q = _context.PersonalExpenses
            .Where(e => e.TenantId == tenantId && e.ExpenseDate >= from && e.ExpenseDate <= to);
        if (source.HasValue) q = q.Where(e => e.Source == source.Value);
        return await q.OrderByDescending(e => e.ExpenseDate).Take(1000).AsNoTracking().ToListAsync(ct); // G485: pagination guard
    }

    public async Task<decimal> GetTotalByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.PersonalExpenses
            .Where(e => e.TenantId == tenantId && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .SumAsync(e => e.Amount, ct);

    public async Task AddAsync(PersonalExpense expense, CancellationToken ct = default)
        => await _context.PersonalExpenses.AddAsync(expense, ct);

    public Task UpdateAsync(PersonalExpense expense, CancellationToken ct = default)
    {
        _context.PersonalExpenses.Update(expense);
        return Task.CompletedTask;
    }
}
