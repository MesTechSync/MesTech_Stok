using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class CashRegisterRepository : ICashRegisterRepository
{
    private readonly AppDbContext _context;

    public CashRegisterRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CashRegister?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.CashRegisters
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<CashRegister>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.CashRegisters
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<CashRegister?> GetDefaultAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.IsDefault && c.IsActive, ct);

    public async Task AddAsync(CashRegister cashRegister, CancellationToken ct = default)
        => await _context.CashRegisters.AddAsync(cashRegister, ct);

    public Task UpdateAsync(CashRegister cashRegister, CancellationToken ct = default)
    {
        _context.CashRegisters.Update(cashRegister);
        return Task.CompletedTask;
    }
}
