namespace MesTech.Application.DTOs.Dashboard;

/// <summary>
/// Unified dashboard summary: 12 KPI + 2 chart + 2 table.
/// Served by GET /api/v1/dashboard/summary.
/// </summary>
public class DashboardSummaryDto
{
    // Satır 1 — Ana metrikler
    public decimal TodaySalesAmount { get; set; }
    public int TodayOrderCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int CriticalStockCount { get; set; }

    // Satır 2 — Platform metrikleri
    public int ActivePlatformCount { get; set; }
    public int PendingShipmentCount { get; set; }
    public decimal MonthlySalesAmount { get; set; }
    public decimal ReturnRate { get; set; }

    // Grafik verileri
    public List<DailySalesPointDto> Last7DaysSales { get; set; } = new();
    public List<PlatformOrderDistDto> PlatformDistribution { get; set; } = new();

    // Tablolar
    public List<RecentOrderItemDto> RecentOrders { get; set; } = new();
    public List<CriticalStockItemDto> CriticalStockItems { get; set; } = new();
}

public class DailySalesPointDto
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int OrderCount { get; set; }
}

public class PlatformOrderDistDto
{
    public string PlatformName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Percentage { get; set; }
}

public class RecentOrderItemDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PlatformName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CriticalStockItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
    public int Deficit => MinimumStock - CurrentStock;
}
