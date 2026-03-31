namespace MesTech.Application.Features.Accounting.Queries.GetExpenseReport;

public sealed class ExpenseReportDto
{
    public decimal TotalExpenses { get; init; }
    public IReadOnlyList<CategoryBreakdownItem> CategoryBreakdown { get; init; } = [];
    public IReadOnlyList<MonthlyTrendItem> MonthlyTrend { get; init; } = [];
}

public sealed class CategoryBreakdownItem
{
    public string Category { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public int Count { get; init; }
}

public sealed class MonthlyTrendItem
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Amount { get; init; }
}
