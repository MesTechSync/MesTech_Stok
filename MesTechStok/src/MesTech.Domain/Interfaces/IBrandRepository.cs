using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken ct = default);
    Task<Brand?> GetByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Brand brand, CancellationToken ct = default);
    Task UpdateAsync(Brand brand, CancellationToken ct = default);
}
