namespace MesTech.Application.DTOs.Reports;

/// <summary>
/// Vergi beyanname ozet raporu satiri — KDV oran bazinda hesaplanan ve indirilecek KDV ozeti.
/// Muhasebe modulundeki TaxSummaryDto'dan farki: bu rapor beyanname hazirligi icin donem bazli ozet sunar.
/// </summary>
public class TaxSummaryReportDto
{
    public string TaxPeriod { get; set; } = string.Empty;
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
    public decimal OutputVat { get; set; }
    public decimal InputVat { get; set; }
    public decimal NetVatPayable { get; set; }
    public int InvoiceCount { get; set; }
    public decimal WithholdingAmount { get; set; }
}
