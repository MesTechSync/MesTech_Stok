using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IProductSetRepository
{
    Task<ProductSet?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<ProductSet>> GetAllAsync(Guid? tenantId = null);
    Task AddAsync(ProductSet productSet);
    Task UpdateAsync(ProductSet productSet);
}
