namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Aylik ozet raporu — satis, komisyon, kargo, gider, vergi ve iade metrikleri.
/// </summary>
public sealed class MonthlySummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCommissions { get; set; }
    public decimal TotalShippingCost { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalTaxDue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalReturns { get; set; }
    public decimal ReturnRate => TotalOrders > 0
        ? Math.Round((decimal)TotalReturns / TotalOrders * 100, 2)
        : 0;
    public decimal AverageOrderValue => TotalOrders > 0
        ? Math.Round(TotalSales / TotalOrders, 2)
        : 0;
    public IReadOnlyList<PlatformSalesDto> SalesByPlatform { get; set; } = [];
}

/// <summary>
/// Platform bazinda satis ozeti.
/// </summary>
public sealed class PlatformSalesDto
{
    public string Platform { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public int OrderCount { get; set; }
    public decimal Commission { get; set; }
}
