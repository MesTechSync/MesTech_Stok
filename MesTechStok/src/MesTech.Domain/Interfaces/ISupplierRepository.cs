using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Supplier>> GetAllAsync();
    Task<IReadOnlyList<Supplier>> GetActiveAsync();
    Task AddAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(Guid id);
}
