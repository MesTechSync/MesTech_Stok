using MesTech.Domain.Entities.Billing;

namespace MesTech.Domain.Interfaces;

public interface IBillingInvoiceRepository
{
    Task<BillingInvoice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BillingInvoice>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<BillingInvoice>> GetOverdueAsync(CancellationToken ct = default);
    Task<int> GetNextSequenceAsync(CancellationToken ct = default);
    Task AddAsync(BillingInvoice invoice, CancellationToken ct = default);
    Task UpdateAsync(BillingInvoice invoice, CancellationToken ct = default);
}
