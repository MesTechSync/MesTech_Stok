namespace MesTech.Application.DTOs.Finance;

public class ProfitLossDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit => TotalRevenue - TotalExpenses;
    public decimal ProfitMarginPercent =>
        TotalRevenue > 0 ? Math.Round(NetProfit / TotalRevenue * 100, 2) : 0;

    public IReadOnlyList<PlatformRevenueDto> RevenueByPlatform { get; set; } = [];
    public IReadOnlyList<ExpenseCategoryDto> ExpenseByCategory { get; set; } = [];
}

public class PlatformRevenueDto
{
    public string Platform { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class ExpenseCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
