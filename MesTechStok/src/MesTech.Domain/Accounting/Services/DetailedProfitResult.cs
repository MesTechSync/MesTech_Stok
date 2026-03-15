namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Detayli P&amp;L sonucu.
/// </summary>
public record DetailedProfitResult
{
    public decimal TotalRevenue { get; init; }
    public decimal TotalCogs { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal GrossMargin { get; init; }
    public decimal TotalCommission { get; init; }
    public decimal TotalCargo { get; init; }
    public decimal TotalWithholding { get; init; }
    public decimal OtherExpenses { get; init; }
    public decimal NetProfit { get; init; }
    public decimal NetMargin { get; init; }
}
