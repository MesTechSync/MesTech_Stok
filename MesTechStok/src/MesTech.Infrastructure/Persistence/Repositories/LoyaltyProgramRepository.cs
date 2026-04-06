using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class LoyaltyProgramRepository : ILoyaltyProgramRepository
{
    private readonly AppDbContext _context;

    public LoyaltyProgramRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LoyaltyProgram?> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.LoyaltyPrograms
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .AsNoTracking().FirstOrDefaultAsync(ct).ConfigureAwait(false);

    public async Task<LoyaltyProgram?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.LoyaltyPrograms.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);

    public async Task AddAsync(LoyaltyProgram program, CancellationToken ct = default)
        => await _context.LoyaltyPrograms.AddAsync(program, ct).ConfigureAwait(false);

    public Task UpdateAsync(LoyaltyProgram program, CancellationToken ct = default)
    {
        _context.LoyaltyPrograms.Update(program);
        return Task.CompletedTask;
    }
}
