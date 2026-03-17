using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetKdvReport;

/// <summary>
/// Basitlestirilmis KDV raporu sorgulama — API tuketicileri icin.
/// </summary>
public record GetKdvReportQuery(Guid TenantId, int Year, int Month)
    : IRequest<KdvReportDto>;
