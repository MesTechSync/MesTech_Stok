using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetRevenueChart;

/// <summary>
/// Gelir grafigi sorgusu — gun bazinda siparis tutari ve sayisi.
/// Cache: 10 dakika (chart verisi yavaş değişir).
/// </summary>
public record GetRevenueChartQuery(Guid TenantId, int Days = 30)
    : IRequest<IReadOnlyList<RevenueChartPointDto>>, ICacheableQuery
{
    public string CacheKey => $"RevenueChart_{TenantId}_{Days}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Gelir grafik noktasi DTO.
/// </summary>
public record RevenueChartPointDto
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
}
