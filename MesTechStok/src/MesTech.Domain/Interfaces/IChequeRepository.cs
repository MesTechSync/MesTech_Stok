using MesTech.Domain.Entities.Finance;

namespace MesTech.Domain.Interfaces;

public interface IChequeRepository
{
    Task<Cheque?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Cheque>> GetByTenantAsync(Guid tenantId, ChequeStatus? status = null, CancellationToken ct = default);
    Task<IReadOnlyList<Cheque>> GetOverdueAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Cheque cheque, CancellationToken ct = default);
    Task UpdateAsync(Cheque cheque, CancellationToken ct = default);
}
