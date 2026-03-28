using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetSalesChartData;

/// <summary>
/// Son N gün, platform bazlı günlük sipariş + gelir grafiği.
/// LiveCharts2 için optimize edilmiş DTO döner.
/// </summary>
public sealed record GetSalesChartDataQuery(
    Guid TenantId,
    int Days = 30,
    string? PlatformCode = null) : IRequest<SalesChartDataDto>;

public sealed class SalesChartDataDto
{
    public IReadOnlyList<string> Labels { get; init; } = [];
    public IReadOnlyList<SalesChartSeriesDto> Series { get; init; } = [];
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
}

public sealed class SalesChartSeriesDto
{
    public string PlatformCode { get; init; } = string.Empty;
    public string PlatformName { get; init; } = string.Empty;
    public IReadOnlyList<decimal> RevenueValues { get; init; } = [];
    public IReadOnlyList<int> OrderCountValues { get; init; } = [];
}
