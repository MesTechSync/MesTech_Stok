namespace MesTech.Application.DTOs.Reports;

/// <summary>
/// Musteri yasam boyu degeri raporu satiri — musteri bazinda siparis sayisi, toplam harcama ve CLV.
/// </summary>
public sealed class CustomerLifetimeValueReportDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime FirstOrderDate { get; set; }
    public DateTime LastOrderDate { get; set; }
    public int DaysSinceLastOrder { get; set; }
    public decimal EstimatedCLV { get; set; }
}
