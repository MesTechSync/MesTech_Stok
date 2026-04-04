using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken ct = default);
    Task<Warehouse?> GetDefaultAsync(CancellationToken ct = default);
    Task AddAsync(Warehouse warehouse, CancellationToken ct = default);
    Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
