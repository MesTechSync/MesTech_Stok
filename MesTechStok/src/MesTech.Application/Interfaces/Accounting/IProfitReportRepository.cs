using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface IProfitReportRepository
{
    Task<ProfitReport?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProfitReport>> GetByPeriodAsync(Guid tenantId, string period, string? platform = null, CancellationToken ct = default);
    Task<ProfitReport?> GetLatestAsync(Guid tenantId, string? platform = null, CancellationToken ct = default);
    Task<IReadOnlyList<ProfitReport>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, string? platform = null, CancellationToken ct = default);
    Task AddAsync(ProfitReport report, CancellationToken ct = default);
}
