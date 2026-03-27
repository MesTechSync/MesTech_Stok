using MesTech.Domain.Entities.Finance;

namespace MesTech.Domain.Interfaces;

public interface IBankAccountRepository
{
    Task<IReadOnlyList<BankAccount>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(BankAccount account, CancellationToken ct = default);
    Task UpdateAsync(BankAccount account, CancellationToken ct = default);
}
