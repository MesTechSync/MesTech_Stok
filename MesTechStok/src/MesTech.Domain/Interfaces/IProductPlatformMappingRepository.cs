using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IProductPlatformMappingRepository
{
    Task<IReadOnlyList<ProductPlatformMapping>> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductPlatformMapping>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<ProductPlatformMapping?> GetByExternalIdAsync(Guid storeId, string externalProductId, CancellationToken ct = default);
    Task<int> CountByStoreIdAsync(Guid storeId, CancellationToken ct = default);
    Task AddAsync(ProductPlatformMapping mapping, CancellationToken ct = default);
    Task UpdateAsync(ProductPlatformMapping mapping, CancellationToken ct = default);
}
