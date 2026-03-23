using MesTech.Domain.Entities.Reporting;

namespace MesTech.Application.Interfaces;

/// <summary>
/// SavedReport veri erisim arayuzu.
/// </summary>
public interface ISavedReportRepository
{
    Task<SavedReport?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SavedReport>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(SavedReport report, CancellationToken ct = default);
    Task DeleteAsync(SavedReport report, CancellationToken ct = default);
}
