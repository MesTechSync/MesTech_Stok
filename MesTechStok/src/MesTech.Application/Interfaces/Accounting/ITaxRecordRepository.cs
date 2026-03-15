using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface ITaxRecordRepository
{
    Task<TaxRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TaxRecord>> GetByPeriodAsync(Guid tenantId, string period, CancellationToken ct = default);
    Task<IReadOnlyList<TaxRecord>> GetUnpaidAsync(Guid tenantId, CancellationToken ct = default);
    Task<decimal> GetTotalTaxByPeriodAsync(Guid tenantId, string period, CancellationToken ct = default);
    Task AddAsync(TaxRecord record, CancellationToken ct = default);
    Task UpdateAsync(TaxRecord record, CancellationToken ct = default);
}
