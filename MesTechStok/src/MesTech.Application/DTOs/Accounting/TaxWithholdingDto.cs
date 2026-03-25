namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Stopaj (tevkifat) kaydi DTO'su.
/// </summary>
public sealed class TaxWithholdingDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal TaxExclusiveAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal WithholdingAmount { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
