namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Kar/zarar hesaplama servisi.
/// </summary>
public class ProfitCalculationService : IProfitCalculationService
{
    /// <inheritdoc />
    public decimal CalculateNetProfit(
        decimal totalRevenue,
        decimal totalCost,
        decimal totalCommission,
        decimal totalCargo,
        decimal totalTax)
    {
        return Math.Round(totalRevenue - totalCost - totalCommission - totalCargo - totalTax, 2);
    }

    /// <inheritdoc />
    public decimal CalculateProfitMargin(decimal revenue, decimal netProfit)
    {
        if (revenue == 0)
            return 0;

        return Math.Round(netProfit / revenue * 100, 2);
    }
}
