using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<Invoice?> GetByOrderIdAsync(Guid orderId);
    Task<IReadOnlyList<Invoice>> GetFailedAsync(int maxCount, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
}
