namespace MesTech.Application.DTOs.Reports;

/// <summary>
/// Stok devir hizi raporu satiri — urun bazinda satis miktari, ortalama stok gunu ve devir orani.
/// </summary>
public sealed class StockTurnoverReportDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int SoldQuantity { get; set; }
    public double AvgStockDays { get; set; }
    public double TurnoverRate { get; set; }
    public double DaysOfSupply { get; set; }
}
