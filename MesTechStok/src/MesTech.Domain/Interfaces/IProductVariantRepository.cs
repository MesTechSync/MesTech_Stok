using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<ProductVariant?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task AddAsync(ProductVariant variant, CancellationToken ct = default);
    Task UpdateAsync(ProductVariant variant, CancellationToken ct = default);
}
