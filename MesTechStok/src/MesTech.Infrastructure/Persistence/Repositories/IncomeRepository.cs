using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class IncomeRepository : IIncomeRepository
{
    private readonly AppDbContext _context;

    public IncomeRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Income?> GetByIdAsync(Guid id)
        => await _context.Incomes
            .AsNoTracking().FirstOrDefaultAsync(i => i.Id == id).ConfigureAwait(false);

    public async Task<IReadOnlyList<Income>> GetAllAsync(Guid? tenantId = null)
        => await _context.Incomes
            .Where(i => tenantId == null || i.TenantId == tenantId.Value)
            .OrderByDescending(i => i.Date)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<Income>> GetByDateRangeAsync(DateTime from, DateTime to, Guid? tenantId = null)
        => await _context.Incomes
            .Where(i => i.Date >= from && i.Date <= to)
            .Where(i => tenantId == null || i.TenantId == tenantId.Value)
            .OrderByDescending(i => i.Date)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync().ConfigureAwait(false);

    public async Task<IReadOnlyList<Income>> GetByTypeAsync(IncomeType type, Guid? tenantId = null)
        => await _context.Incomes
            .Where(i => i.IncomeType == type)
            .Where(i => tenantId == null || i.TenantId == tenantId.Value)
            .OrderByDescending(i => i.Date)
            .Take(5000) // G485
            .AsNoTracking().ToListAsync().ConfigureAwait(false);

    public async Task<bool> ExistsByOrderIdAsync(Guid tenantId, Guid orderId, CancellationToken ct = default)
        => await _context.Incomes
            .AnyAsync(i => i.TenantId == tenantId && i.OrderId == orderId, ct).ConfigureAwait(false);

    public async Task AddAsync(Income income)
        => await _context.Incomes.AddAsync(income).ConfigureAwait(false);

    public Task UpdateAsync(Income income)
    {
        _context.Incomes.Update(income);
        return Task.CompletedTask;
    }
}
