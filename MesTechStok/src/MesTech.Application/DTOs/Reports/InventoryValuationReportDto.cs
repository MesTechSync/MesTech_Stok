namespace MesTech.Application.DTOs.Reports;

/// <summary>
/// Envanter degerleme raporu satiri — urun bazinda stok miktari, birim maliyet ve toplam deger.
/// </summary>
public sealed class InventoryValuationReportDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalSaleValue { get; set; }
    public decimal PotentialProfit { get; set; }
}
