using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Interfaces.Accounting;

public interface IPenaltyRecordRepository
{
    Task<PenaltyRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PenaltyRecord>> GetAllAsync(Guid tenantId, PenaltySource? source = null, CancellationToken ct = default);
    Task AddAsync(PenaltyRecord record, CancellationToken ct = default);
    Task UpdateAsync(PenaltyRecord record, CancellationToken ct = default);
}
