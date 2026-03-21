using MesTech.Domain.Entities.Finance;

namespace MesTech.Domain.Interfaces;

public interface ICashRegisterRepository
{
    Task<CashRegister?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CashRegister>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<CashRegister?> GetDefaultAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(CashRegister cashRegister, CancellationToken ct = default);
    Task UpdateAsync(CashRegister cashRegister, CancellationToken ct = default);
}
