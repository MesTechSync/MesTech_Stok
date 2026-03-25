namespace MesTech.Application.DTOs.Reports;

/// <summary>
/// Siparis karsilama raporu satiri — gonderi suresi analizi (siparis → kargo → teslimat).
/// </summary>
public sealed class OrderFulfillmentReportDto
{
    public string Platform { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public double AvgOrderToShipHours { get; set; }
    public double AvgShipToDeliverDays { get; set; }
    public double AvgTotalFulfillmentDays { get; set; }
    public double FulfillmentRate { get; set; }
}
