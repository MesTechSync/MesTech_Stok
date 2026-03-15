using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface ICommissionRecordRepository
{
    Task<IReadOnlyList<CommissionRecord>> GetByPlatformAsync(Guid tenantId, string platform, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<decimal> GetTotalCommissionAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(CommissionRecord record, CancellationToken ct = default);
}
