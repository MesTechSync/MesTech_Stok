namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Kar/zarar hesaplama servisi arayuzu.
/// FIFO COGS destekli detayli P&amp;L hesaplama.
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

    /// <summary>
    /// Detayli P&amp;L hesaplama — FIFO COGS destekli.
    /// NetKar = Gelir - COGS(FIFO) - Komisyon - Kargo - Stopaj - DigerGider
    /// </summary>
    DetailedProfitResult CalculateDetailed(
        decimal totalRevenue,
        decimal totalCogs,
        decimal totalCommission,
        decimal totalCargo,
        decimal totalWithholding,
        decimal otherExpenses);

    /// <summary>
    /// FIFO yaklasimi ile satis maliyeti (COGS) hesaplar.
    /// Product.PurchasePrice mevcutsa FIFO, degilse ortalama maliyet kullanir.
    /// </summary>
    decimal CalculateFifoCogs(IReadOnlyList<CostLayerInput> costLayers, int quantitySold);
}

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

/// <summary>
/// FIFO maliyet katmani — alis partisi bilgisi.
/// </summary>
public record CostLayerInput(int Quantity, decimal UnitCost);
