using MesTech.Application.Behaviors;
using MesTech.Application.DTOs.Dashboard;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;

/// <summary>
/// Unified 12-KPI dashboard özet sorgusu.
/// Mevcut 6 dashboard query'ye DOKUNMAZ — ek aggregation katmanı.
/// Cache: 2 dakika (dashboard sık yenilenir).
/// </summary>
public record GetDashboardSummaryQuery(Guid TenantId) : IRequest<DashboardSummaryDto>, ICacheableQuery
{
    public string CacheKey => $"DashboardSummary_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}
