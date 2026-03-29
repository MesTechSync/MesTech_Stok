using MediatR;
using MesTech.Application.Behaviors;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;

/// <summary>
/// Aylik ozet raporu sorgulama — satis, komisyon, gider, vergi metrikleri.
/// </summary>
public record GetMonthlySummaryQuery(int Year, int Month, Guid TenantId)
    : IRequest<MonthlySummaryDto>, ICacheableQuery
{
    public string CacheKey => $"MonthlySummary_{TenantId}_{Year}_{Month}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
