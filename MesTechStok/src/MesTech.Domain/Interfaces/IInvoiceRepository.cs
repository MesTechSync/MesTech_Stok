using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetFailedAsync(int maxCount, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Invoice invoice, CancellationToken ct = default);
    Task UpdateAsync(Invoice invoice, CancellationToken ct = default);
}
