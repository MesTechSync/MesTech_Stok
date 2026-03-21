using MesTech.Domain.Entities.Crm;

namespace MesTech.Domain.Interfaces;

public interface ILoyaltyRepository
{
    Task<LoyaltyProgram?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<LoyaltyProgram>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(LoyaltyProgram program, CancellationToken ct = default);
    Task<IReadOnlyList<LoyaltyTransaction>> GetTransactionsByCustomerAsync(Guid tenantId, Guid customerId, CancellationToken ct = default);
    Task AddTransactionAsync(LoyaltyTransaction transaction, CancellationToken ct = default);
}
