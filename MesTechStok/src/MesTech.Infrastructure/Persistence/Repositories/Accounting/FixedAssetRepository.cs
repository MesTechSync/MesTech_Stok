using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories.Accounting;

public class FixedAssetRepository : IFixedAssetRepository
{
    private readonly AppDbContext _context;

    public FixedAssetRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<FixedAsset?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.FixedAssets
            .FirstOrDefaultAsync(a => a.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<FixedAsset>> GetAllAsync(Guid tenantId, bool? isActive = null, CancellationToken ct = default)
        => await _context.FixedAssets
            .Where(a => a.TenantId == tenantId)
            .Where(a => isActive == null || a.IsActive == isActive.Value)
            .OrderBy(a => a.Name)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(FixedAsset asset, CancellationToken ct = default)
        => await _context.FixedAssets.AddAsync(asset, ct).ConfigureAwait(false);

    public async Task UpdateAsync(FixedAsset asset, CancellationToken ct = default)
    {
        _context.FixedAssets.Update(asset);
        await Task.CompletedTask;
    }
}
