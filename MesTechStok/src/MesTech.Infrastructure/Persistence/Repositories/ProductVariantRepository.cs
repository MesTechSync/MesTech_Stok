using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ProductVariantRepository : IProductVariantRepository
{
    private readonly AppDbContext _context;

    public ProductVariantRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<ProductVariant?> GetByIdAsync(Guid id)
        => await _context.ProductVariants
            .AsNoTracking().FirstOrDefaultAsync(v => v.Id == id)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(Guid productId)
        => await _context.ProductVariants
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.SKU)
            .AsNoTracking().ToListAsync()
            .ConfigureAwait(false);

    public async Task<ProductVariant?> GetBySkuAsync(string sku)
        => await _context.ProductVariants
            .AsNoTracking().FirstOrDefaultAsync(v => v.SKU == sku)
            .ConfigureAwait(false);

    public async Task AddAsync(ProductVariant variant)
        => await _context.ProductVariants.AddAsync(variant).ConfigureAwait(false);

    public Task UpdateAsync(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
        return Task.CompletedTask;
    }
}
