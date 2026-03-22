using MesTech.Domain.Entities.Crm;

namespace MesTech.Domain.Interfaces;

public interface ILoyaltyProgramRepository
{
    Task<LoyaltyProgram?> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<LoyaltyProgram?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(LoyaltyProgram program, CancellationToken ct = default);
    Task UpdateAsync(LoyaltyProgram program, CancellationToken ct = default);
}
