using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task UpdateAsync(Customer customer, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
