using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class BankAccountRepository : IBankAccountRepository
{
    private readonly AppDbContext _context;

    public BankAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BankAccount>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.BankAccounts
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.BankName)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.BankAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct).ConfigureAwait(false);

    public async Task AddAsync(BankAccount account, CancellationToken ct = default)
        => await _context.BankAccounts.AddAsync(account, ct).ConfigureAwait(false);

    public Task UpdateAsync(BankAccount account, CancellationToken ct = default)
    {
        _context.BankAccounts.Update(account);
        return Task.CompletedTask;
    }
}
