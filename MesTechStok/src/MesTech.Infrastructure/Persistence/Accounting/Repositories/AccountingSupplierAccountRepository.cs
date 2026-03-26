using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class AccountingSupplierAccountRepository : IAccountingSupplierAccountRepository
{
    private readonly AppDbContext _context;
    public AccountingSupplierAccountRepository(AppDbContext context) => _context = context;

    public async Task<AccountingSupplierAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.AccountingSupplierAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<AccountingSupplierAccount?> GetBySupplierIdAsync(Guid tenantId, Guid supplierId, CancellationToken ct = default)
        => await _context.AccountingSupplierAccounts
            .AsNoTracking().FirstOrDefaultAsync(a => a.TenantId == tenantId && a.SupplierId == supplierId, ct);

    public async Task<IReadOnlyList<AccountingSupplierAccount>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.AccountingSupplierAccounts
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.Name)
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(AccountingSupplierAccount account, CancellationToken ct = default)
        => await _context.AccountingSupplierAccounts.AddAsync(account, ct);

    public Task UpdateAsync(AccountingSupplierAccount account, CancellationToken ct = default)
    {
        _context.AccountingSupplierAccounts.Update(account);
        return Task.CompletedTask;
    }
}
