using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Ba/Bs kayit repository arayuzu — VUK 396.
/// </summary>
public interface IBaBsRecordRepository
{
    Task<BaBsRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BaBsRecord>> GetAllAsync(Guid tenantId, int? year = null, int? month = null, CancellationToken ct = default);
    Task AddAsync(BaBsRecord record, CancellationToken ct = default);
}
