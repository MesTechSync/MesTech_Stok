using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(Guid productId);
    Task<ProductVariant?> GetBySkuAsync(string sku);
    Task AddAsync(ProductVariant variant);
    Task UpdateAsync(ProductVariant variant);
}
