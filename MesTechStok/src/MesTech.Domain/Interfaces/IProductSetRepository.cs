using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IProductSetRepository
{
    Task<ProductSet?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductSet>> GetAllAsync(Guid? tenantId = null, CancellationToken ct = default);
    Task AddAsync(ProductSet productSet, CancellationToken ct = default);
    Task UpdateAsync(ProductSet productSet, CancellationToken ct = default);
}
