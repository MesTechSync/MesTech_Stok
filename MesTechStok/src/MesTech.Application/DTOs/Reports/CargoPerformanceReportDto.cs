namespace MesTech.Application.DTOs.Reports;

/// <summary>
/// Kargo saglayici performans raporu satiri — gonderi sayisi, ortalama teslimat ve maliyet.
/// </summary>
public class CargoPerformanceReportDto
{
    public string CargoProvider { get; set; } = string.Empty;
    public int ShipmentCount { get; set; }
    public double AvgDeliveryDays { get; set; }
    public decimal AvgCost { get; set; }
    public double SuccessRate { get; set; }
}
