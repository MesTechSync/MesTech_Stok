using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface ITaxWithholdingRepository
{
    Task<IReadOnlyList<TaxWithholding>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken ct = default);
    Task<decimal> GetTotalWithholdingAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(TaxWithholding withholding, CancellationToken ct = default);
    Task<IReadOnlyList<TaxWithholding>> GetAllAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);
}
