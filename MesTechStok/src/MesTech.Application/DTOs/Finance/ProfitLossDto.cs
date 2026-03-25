namespace MesTech.Application.DTOs.Finance;

public sealed class ProfitLossDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }

    /// <summary>Toplam vergi tutari (siparis KDV'lerinden).</summary>
    public decimal TotalTax { get; set; }

    /// <summary>Toplam maas gideri (SalaryRecord toplami).</summary>
    public decimal TotalSalary { get; set; }

    /// <summary>Toplam sabit gider (FixedExpense toplami).</summary>
    public decimal TotalFixedExpense { get; set; }

    /// <summary>Toplam ceza tutari (PenaltyRecord toplami).</summary>
    public decimal TotalPenalty { get; set; }

    /// <summary>Brut kar = TotalRevenue - TotalExpenses.</summary>
    public decimal GrossProfit => TotalRevenue - TotalExpenses;

    /// <summary>Net kar = GrossProfit - TotalTax - TotalSalary - TotalFixedExpense - TotalPenalty.</summary>
    public decimal NetProfit => GrossProfit - TotalTax - TotalSalary - TotalFixedExpense - TotalPenalty;

    public decimal ProfitMarginPercent =>
        TotalRevenue > 0 ? Math.Round(NetProfit / TotalRevenue * 100, 2) : 0;

    public IReadOnlyList<PlatformRevenueDto> RevenueByPlatform { get; set; } = [];
    public IReadOnlyList<ExpenseCategoryDto> ExpenseByCategory { get; set; } = [];
}

public sealed class PlatformRevenueDto
{
    public string Platform { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public sealed class ExpenseCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
