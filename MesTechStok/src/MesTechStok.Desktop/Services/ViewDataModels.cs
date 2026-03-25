using System;
using System.Collections.Generic;

namespace MesTechStok.Desktop.Services;

public class CustomerItem
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public DateTime LastOrderDate { get; set; }
    public decimal TotalPurchases { get; set; }
    public bool IsActive { get; set; } = true;
    public string FormattedTotalPurchases => $"₺{TotalPurchases:N2}";
    public string FormattedRegistrationDate => RegistrationDate.ToString("dd.MM.yyyy");
    public string FormattedLastOrderDate => LastOrderDate.ToString("dd.MM.yyyy");
    public string StatusIcon => IsActive ? "✅" : "❌";
}

public class InventoryItem
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int Stock => StockQuantity;
    public int MinimumStock { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Price => UnitPrice;
    public decimal TotalValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public class OrderItem
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string ProductsList { get; set; } = string.Empty;
    public string FormattedDate => OrderDate.ToString("dd.MM.yyyy HH:mm");
    public string FormattedAmount => $"₺{TotalAmount:N2}";
    public string FormattedLastUpdate => OrderDate.ToString("dd.MM.yyyy HH:mm");
    public string StatusIcon => Status switch
    {
        OrderStatus.Pending => "⏳",
        OrderStatus.Processing => "🔄",
        OrderStatus.Completed => "✅",
        OrderStatus.Cancelled => "❌",
        _ => "❓"
    };
}

public class ReportItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class StockMovement
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime MovementDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class InventoryStatistics
{
    public decimal TotalInventoryValue { get; set; }
    public int LowStockCount { get; set; }
    public int TodayMovements { get; set; }
    public double StockAccuracy { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class DashboardSummary
{
    public decimal TotalRevenue { get; set; }
    public int TotalSales { get; set; }
    public decimal StockValue { get; set; }
    public List<TopProductItem> TopProducts { get; set; } = new();
    public List<LowStockItem> LowStockItems { get; set; } = new();
}

public class TopProductItem
{
    public string Name { get; set; } = string.Empty;
    public int Sales { get; set; }
}

public class LowStockItem
{
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
}

public class DailyRevenueItem
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
}

public class WeeklySalesData
{
    public List<DailySalesData> DailySales { get; set; } = new();
    public decimal WeeklyTotal { get; set; }
    public decimal DailyAverage { get; set; }
}

public class DailySalesData
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string DayName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
}

public class DatabaseInfo
{
    public string Version { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int TableCount { get; set; }
    public string Status { get; set; } = "OK";

    public override string ToString() => $"v{Version} | {SizeBytes / 1024}KB | {TableCount} tables | {Status}";
    public static implicit operator string(DatabaseInfo info) => info?.ToString() ?? string.Empty;
}

// CustomerStatistics is in MesTechStok.Core.Services.Abstract namespace

public class LogAnalysisResult
{
    public List<string> EncodingIssues { get; set; } = new();
    public List<string> PerformanceIssues { get; set; } = new();
    public List<string> SecurityIssues { get; set; } = new();
    public int TotalFilesAnalyzed { get; set; }
    public int TotalIssues => EncodingIssues.Count + PerformanceIssues.Count + SecurityIssues.Count;
    public bool HasIssues => TotalIssues > 0;
}
