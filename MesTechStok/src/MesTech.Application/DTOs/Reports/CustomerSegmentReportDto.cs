namespace MesTech.Application.DTOs.Reports;

/// <summary>
/// Musteri segment raporu satiri — segment bazinda musteri sayisi, ortalama siparis tutari ve toplam gelir.
/// </summary>
public class CustomerSegmentReportDto
{
    public string Segment { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal AvgOrderValue { get; set; }
    public decimal TotalRevenue { get; set; }
}
