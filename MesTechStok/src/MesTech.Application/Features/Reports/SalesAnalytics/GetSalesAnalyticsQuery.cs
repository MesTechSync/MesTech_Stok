using MediatR;

namespace MesTech.Application.Features.Reports.SalesAnalytics;

public record GetSalesAnalyticsQuery(
    Guid TenantId,
    DateTime From,
    DateTime To
) : IRequest<SalesAnalyticsDto>;

public sealed class SalesAnalyticsDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal ConversionRate { get; set; }
    public int TotalProductsSold { get; set; }
    public string TopSellingPlatform { get; set; } = string.Empty;

    public List<DailySalesDto> DailySales { get; set; } = new();
    public List<PlatformSalesDto> PlatformBreakdown { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
}

public sealed class DailySalesDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
}

public sealed class PlatformSalesDto
{
    public string Platform { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
    public decimal Percentage { get; set; }
}

public sealed class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}
