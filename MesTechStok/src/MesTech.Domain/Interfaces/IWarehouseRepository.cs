using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Warehouse>> GetAllAsync();
    Task<Warehouse?> GetDefaultAsync();
    Task AddAsync(Warehouse warehouse);
    Task UpdateAsync(Warehouse warehouse);
    Task DeleteAsync(Guid id);
}
