using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class PlatformCommissionRepository : IPlatformCommissionRepository
{
    private readonly AppDbContext _context;

    public PlatformCommissionRepository(AppDbContext context) => _context = context;

    public async Task<PlatformCommission?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.PlatformCommissions
            .AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<PlatformCommission>> GetByPlatformAsync(
        Guid tenantId,
        PlatformType? platform = null,
        bool? isActive = null,
        CancellationToken ct = default)
    {
        var query = _context.PlatformCommissions
            .Where(c => c.TenantId == tenantId);

        if (platform.HasValue)
            query = query.Where(c => c.Platform == platform.Value);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        return await query.AsNoTracking().ToListAsync(ct);
    }

    public async Task AddAsync(PlatformCommission commission, CancellationToken ct = default)
        => await _context.PlatformCommissions.AddAsync(commission, ct);

    public Task UpdateAsync(PlatformCommission commission, CancellationToken ct = default)
    {
        _context.PlatformCommissions.Update(commission);
        return Task.CompletedTask;
    }
}
