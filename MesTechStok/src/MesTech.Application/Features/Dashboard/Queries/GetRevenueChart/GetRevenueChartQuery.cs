using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetRevenueChart;

/// <summary>
/// Gelir grafigi sorgusu — gun bazinda siparis tutari ve sayisi.
/// </summary>
public record GetRevenueChartQuery(Guid TenantId, int Days = 30)
    : IRequest<IReadOnlyList<RevenueChartPointDto>>;

/// <summary>
/// Gelir grafik noktasi DTO.
/// </summary>
public record RevenueChartPointDto
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
}
