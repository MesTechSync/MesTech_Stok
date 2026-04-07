using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IProductMediaRepository
{
    Task<IReadOnlyList<ProductMedia>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task AddAsync(ProductMedia media, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
