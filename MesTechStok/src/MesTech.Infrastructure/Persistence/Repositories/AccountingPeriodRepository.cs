using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class AccountingPeriodRepository : IAccountingPeriodRepository
{
    private readonly AppDbContext _context;

    public AccountingPeriodRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<IReadOnlyList<AccountingPeriod>> GetByTenantAsync(
        Guid tenantId, int? year = null, CancellationToken ct = default)
    {
        var query = _context.AccountingPeriods.Where(p => p.TenantId == tenantId);
        if (year.HasValue)
            query = query.Where(p => p.Year == year.Value);
        return await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<AccountingPeriod?> GetByYearMonthAsync(
        Guid tenantId, int year, int month, CancellationToken ct = default)
        => await _context.AccountingPeriods
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Year == year && p.Month == month, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(AccountingPeriod period, CancellationToken ct = default)
        => await _context.AccountingPeriods.AddAsync(period, ct).ConfigureAwait(false);
}
