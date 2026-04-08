using MesTech.Domain.Entities.Reporting;

namespace MesTech.Application.Interfaces.Reporting;

public interface IReportDefinitionRepository
{
    Task<ReportDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ReportDefinition>> GetByTenantAsync(Guid tenantId, ReportType? type = null, CancellationToken ct = default);
    Task AddAsync(ReportDefinition definition, CancellationToken ct = default);
    Task UpdateAsync(ReportDefinition definition, CancellationToken ct = default);
}
