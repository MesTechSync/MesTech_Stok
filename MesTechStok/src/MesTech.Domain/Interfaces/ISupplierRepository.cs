using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Supplier>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(Supplier supplier, CancellationToken ct = default);
    Task UpdateAsync(Supplier supplier, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
