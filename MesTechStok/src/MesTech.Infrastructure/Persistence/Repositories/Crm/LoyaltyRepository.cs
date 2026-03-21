using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories.Crm;

public class LoyaltyRepository : ILoyaltyRepository
{
    private readonly AppDbContext _context;

    public LoyaltyRepository(AppDbContext context) => _context = context;

    public async Task<LoyaltyProgram?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Set<LoyaltyProgram>()
            .AsNoTracking()
            .FirstOrDefaultAsync(lp => lp.Id == id && !lp.IsDeleted, ct);

    public async Task<IReadOnlyList<LoyaltyProgram>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Set<LoyaltyProgram>()
            .AsNoTracking()
            .Where(lp => lp.TenantId == tenantId && !lp.IsDeleted)
            .OrderByDescending(lp => lp.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(LoyaltyProgram program, CancellationToken ct = default)
    {
        await _context.Set<LoyaltyProgram>().AddAsync(program, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<LoyaltyTransaction>> GetTransactionsByCustomerAsync(
        Guid tenantId, Guid customerId, CancellationToken ct = default)
        => await _context.Set<LoyaltyTransaction>()
            .AsNoTracking()
            .Where(lt => lt.TenantId == tenantId && lt.CustomerId == customerId && !lt.IsDeleted)
            .OrderByDescending(lt => lt.CreatedAt)
            .ToListAsync(ct);

    public async Task AddTransactionAsync(LoyaltyTransaction transaction, CancellationToken ct = default)
    {
        await _context.Set<LoyaltyTransaction>().AddAsync(transaction, ct);
        await _context.SaveChangesAsync(ct);
    }
}
