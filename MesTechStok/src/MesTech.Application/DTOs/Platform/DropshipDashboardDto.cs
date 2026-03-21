namespace MesTech.Application.DTOs.Platform;

/// <summary>
/// Dropship Dashboard data transfer object.
/// </summary>
public class DropshipDashboardDto
{
    public int ActiveSuppliers { get; set; }
    public int ActiveFeeds { get; set; }
    public int TotalDropshipProducts { get; set; }
    public int PendingOrders { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal MonthlyProfit { get; set; }
    public decimal AverageMargin { get; set; }
    public List<SupplierPerformanceDto> TopSuppliers { get; set; } = new();
}

public class SupplierPerformanceDto
{
    public Guid SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal AvgMargin { get; set; }
}
