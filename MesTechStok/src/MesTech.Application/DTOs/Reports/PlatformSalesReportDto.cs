namespace MesTech.Application.DTOs.Reports;

/// <summary>
/// Platform bazli satis raporu satiri — siparis, gelir, iade, komisyon ve net gelir ozetler.
/// </summary>
public class PlatformSalesReportDto
{
    public string Platform { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int Returns { get; set; }
    public decimal Commissions { get; set; }
    public decimal NetRevenue { get; set; }
}
