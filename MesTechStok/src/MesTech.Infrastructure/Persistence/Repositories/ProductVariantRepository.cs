using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ProductVariantRepository : IProductVariantRepository
{
    private readonly AppDbContext _context;

    public ProductVariantRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ProductVariants
            .AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _context.ProductVariants
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.SKU)
            .Take(1000) // G485: pagination guard
            .AsNoTracking().ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<ProductVariant?> GetBySkuAsync(string sku, CancellationToken ct = default)
        => await _context.ProductVariants
            .AsNoTracking().FirstOrDefaultAsync(v => v.SKU == sku, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(ProductVariant variant, CancellationToken ct = default)
        => await _context.ProductVariants.AddAsync(variant, ct).ConfigureAwait(false);

    public Task UpdateAsync(ProductVariant variant, CancellationToken ct = default)
    {
        _context.ProductVariants.Update(variant);
        return Task.CompletedTask;
    }
}
