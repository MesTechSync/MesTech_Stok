using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ProductPlatformMappingRepository : IProductPlatformMappingRepository
{
    private readonly AppDbContext _db;

    public ProductPlatformMappingRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductPlatformMapping>> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default)
        => await _db.Set<ProductPlatformMapping>()
            .Where(m => m.StoreId == storeId && !m.IsDeleted)
            .Take(5000) // G485: pagination guard
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductPlatformMapping>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _db.Set<ProductPlatformMapping>()
            .Where(m => m.ProductId == productId && !m.IsDeleted)
            .Take(1000) // G485: pagination guard
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<ProductPlatformMapping?> GetByExternalIdAsync(Guid storeId, string externalProductId, CancellationToken ct = default)
        => await _db.Set<ProductPlatformMapping>()
            .FirstOrDefaultAsync(m => m.StoreId == storeId && m.ExternalProductId == externalProductId && !m.IsDeleted, ct).ConfigureAwait(false);

    public async Task<int> CountByStoreIdAsync(Guid storeId, CancellationToken ct = default)
        => await _db.Set<ProductPlatformMapping>()
            .CountAsync(m => m.StoreId == storeId && !m.IsDeleted, ct).ConfigureAwait(false);

    public async Task AddAsync(ProductPlatformMapping mapping, CancellationToken ct = default)
        => await _db.Set<ProductPlatformMapping>().AddAsync(mapping, ct).ConfigureAwait(false);

    public Task UpdateAsync(ProductPlatformMapping mapping, CancellationToken ct = default)
    {
        _db.Set<ProductPlatformMapping>().Update(mapping);
        return Task.CompletedTask;
    }
}
