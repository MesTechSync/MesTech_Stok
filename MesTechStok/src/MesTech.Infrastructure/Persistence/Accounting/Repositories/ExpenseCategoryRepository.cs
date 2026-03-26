using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using ExpenseCat = MesTech.Domain.Accounting.Entities.ExpenseCategory;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class ExpenseCategoryRepository : IExpenseCategoryRepository
{
    private readonly AppDbContext _context;
    public ExpenseCategoryRepository(AppDbContext context) => _context = context;

    public async Task<ExpenseCat?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.AccountingExpenseCategories.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<ExpenseCat>> GetAllAsync(Guid tenantId, bool? isActive = null, CancellationToken ct = default)
    {
        var q = _context.AccountingExpenseCategories.Where(c => c.TenantId == tenantId);
        if (isActive.HasValue) q = q.Where(c => c.IsActive == isActive.Value);
        return await q.OrderBy(c => c.Name).AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ExpenseCat>> GetByParentIdAsync(Guid tenantId, Guid? parentId, CancellationToken ct = default)
        => await _context.AccountingExpenseCategories
            .Where(c => c.TenantId == tenantId && c.ParentId == parentId)
            .OrderBy(c => c.Name).AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(ExpenseCat category, CancellationToken ct = default)
        => await _context.AccountingExpenseCategories.AddAsync(category, ct);

    public Task UpdateAsync(ExpenseCat category, CancellationToken ct = default)
    {
        _context.AccountingExpenseCategories.Update(category);
        return Task.CompletedTask;
    }
}
