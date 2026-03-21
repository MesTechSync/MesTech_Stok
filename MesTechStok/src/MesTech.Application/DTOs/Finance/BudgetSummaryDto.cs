namespace MesTech.Application.DTOs.Finance;

/// <summary>
/// Budget Summary data transfer object.
/// </summary>
public class BudgetSummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Remaining => TotalBudget - TotalSpent;
    public decimal UtilizationPercent => TotalBudget > 0 ? Math.Round(TotalSpent / TotalBudget * 100, 2) : 0;
    public List<BudgetCategoryDto> Categories { get; set; } = new();
}

public class BudgetCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining => Budget - Spent;
}
