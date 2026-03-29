using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetKdvReport;

/// <summary>
/// Basitlestirilmis KDV raporu sorgulama — API tuketicileri icin.
/// </summary>
public record GetKdvReportQuery(Guid TenantId, int Year, int Month)
    : IRequest<KdvReportDto>, ICacheableQuery
{
    public string CacheKey => $"KdvReport_{TenantId}_{Year}_{Month}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}
