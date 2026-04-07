using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IProductSpecificationRepository
{
    Task<IReadOnlyList<ProductSpecification>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task AddAsync(ProductSpecification spec, CancellationToken ct = default);
    Task UpdateAsync(ProductSpecification spec, CancellationToken ct = default);
}
