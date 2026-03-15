using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface ISettlementBatchRepository
{
    Task<SettlementBatch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SettlementBatch>> GetByPlatformAsync(Guid tenantId, string platform, CancellationToken ct = default);
    Task<IReadOnlyList<SettlementBatch>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(SettlementBatch batch, CancellationToken ct = default);
    Task UpdateAsync(SettlementBatch batch, CancellationToken ct = default);
}
