namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Kar/zarar hesaplama servisi.
/// FIFO COGS destekli detayli P&amp;L hesaplama.
/// </summary>
public sealed class ProfitCalculationService : IProfitCalculationService
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

    /// <inheritdoc />
    public DetailedProfitResult CalculateDetailed(
        decimal totalRevenue,
        decimal totalCogs,
        decimal totalCommission,
        decimal totalCargo,
        decimal totalWithholding,
        decimal otherExpenses)
    {
        var grossProfit = totalRevenue - totalCogs;
        var grossMargin = totalRevenue != 0
            ? Math.Round(grossProfit / totalRevenue * 100, 2)
            : 0m;

        var netProfit = grossProfit - totalCommission - totalCargo - totalWithholding - otherExpenses;
        var netMargin = totalRevenue != 0
            ? Math.Round(netProfit / totalRevenue * 100, 2)
            : 0m;

        return new DetailedProfitResult
        {
            TotalRevenue = Math.Round(totalRevenue, 2),
            TotalCogs = Math.Round(totalCogs, 2),
            GrossProfit = Math.Round(grossProfit, 2),
            GrossMargin = grossMargin,
            TotalCommission = Math.Round(totalCommission, 2),
            TotalCargo = Math.Round(totalCargo, 2),
            TotalWithholding = Math.Round(totalWithholding, 2),
            OtherExpenses = Math.Round(otherExpenses, 2),
            NetProfit = Math.Round(netProfit, 2),
            NetMargin = netMargin
        };
    }

    /// <inheritdoc />
    public decimal CalculateFifoCogs(IReadOnlyList<CostLayerInput> costLayers, int quantitySold)
    {
        if (costLayers == null || costLayers.Count == 0 || quantitySold <= 0)
            return 0m;

        var totalCogs = 0m;
        var remaining = quantitySold;

        foreach (var layer in costLayers)
        {
            if (remaining <= 0) break;

            var take = Math.Min(remaining, layer.Quantity);
            totalCogs += take * layer.UnitCost;
            remaining -= take;
        }

        // If not enough cost layers, use average cost for remainder
        if (remaining > 0 && costLayers.Count > 0)
        {
            var totalQty = costLayers.Sum(l => l.Quantity);
            var totalCost = costLayers.Sum(l => l.Quantity * l.UnitCost);
            var avgCost = totalQty > 0 ? totalCost / totalQty : 0m;
            totalCogs += remaining * avgCost;
        }

        return Math.Round(totalCogs, 2);
    }
}
