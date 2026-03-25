namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Tax Summary data transfer object.
/// </summary>
public sealed class TaxSummaryDto
{
    public decimal TotalTaxable { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalWithholding { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalUnpaid { get; set; }
    public List<TaxRecordDto> Records { get; set; } = new();
}

public sealed class TaxRecordDto
{
    public Guid Id { get; set; }
    public string Period { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsPaid { get; set; }
}
