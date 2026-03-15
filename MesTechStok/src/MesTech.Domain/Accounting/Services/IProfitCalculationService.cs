namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Kar/zarar hesaplama servisi arayuzu.
/// </summary>
public interface IProfitCalculationService
{
    /// <summary>
    /// Net kari hesaplar.
    /// NetKar = Gelir - Maliyet - Komisyon - Kargo - Vergi
    /// </summary>
    decimal CalculateNetProfit(
        decimal totalRevenue,
        decimal totalCost,
        decimal totalCommission,
        decimal totalCargo,
        decimal totalTax);

    /// <summary>
    /// Kar marjini hesaplar (yuzde olarak).
    /// </summary>
    decimal CalculateProfitMargin(decimal revenue, decimal netProfit);
}
