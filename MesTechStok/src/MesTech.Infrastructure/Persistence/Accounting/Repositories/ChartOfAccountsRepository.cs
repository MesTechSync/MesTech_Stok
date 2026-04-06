using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class ChartOfAccountsRepository : IChartOfAccountsRepository
{
    private readonly AppDbContext _context;
    public ChartOfAccountsRepository(AppDbContext context) => _context = context;

    public async Task<ChartOfAccounts?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ChartOfAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<ChartOfAccounts?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct = default)
        => await _context.ChartOfAccounts
            .AsNoTracking().FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code == code, ct);

    public async Task<IReadOnlyList<ChartOfAccounts>> GetAllAsync(Guid tenantId, bool? isActive = null, CancellationToken ct = default)
    {
        var q = _context.ChartOfAccounts.Where(a => a.TenantId == tenantId);
        if (isActive.HasValue) q = q.Where(a => a.IsActive == isActive.Value);
        return await q.OrderBy(a => a.Code).Take(1000).AsNoTracking().ToListAsync(ct); // G485: pagination guard
    }

    public async Task<IReadOnlyList<ChartOfAccounts>> GetByParentIdAsync(Guid tenantId, Guid? parentId, CancellationToken ct = default)
        => await _context.ChartOfAccounts
            .Where(a => a.TenantId == tenantId && a.ParentId == parentId)
            .OrderBy(a => a.Code).Take(1000).AsNoTracking().ToListAsync(ct); // G485: pagination guard

    public async Task AddAsync(ChartOfAccounts account, CancellationToken ct = default)
        => await _context.ChartOfAccounts.AddAsync(account, ct);

    public Task UpdateAsync(ChartOfAccounts account, CancellationToken ct = default)
    {
        _context.ChartOfAccounts.Update(account);
        return Task.CompletedTask;
    }
}
