using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Interfaces.Accounting;

public interface ICashFlowEntryRepository
{
    Task<CashFlowEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CashFlowEntry>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CashFlowDirection? direction = null, CancellationToken ct = default);
    Task<decimal> GetTotalByDirectionAsync(Guid tenantId, CashFlowDirection direction, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(CashFlowEntry entry, CancellationToken ct = default);
}
