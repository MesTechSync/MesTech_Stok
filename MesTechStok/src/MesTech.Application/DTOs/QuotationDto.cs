namespace MesTech.Application.DTOs;

/// <summary>
/// Quotation data transfer object.
/// </summary>
public class QuotationDto
{
    public Guid Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime QuotationDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxNumber { get; set; }
    public string? CustomerEmail { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Notes { get; set; }
    public Guid? ConvertedInvoiceId { get; set; }
    public List<QuotationLineDto> Lines { get; set; } = [];
}

public class QuotationLineDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Description { get; set; }
}
