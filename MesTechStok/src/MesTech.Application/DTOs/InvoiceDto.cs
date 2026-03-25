using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs;

/// <summary>
/// Invoice data transfer object.
/// </summary>
public sealed class InvoiceDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType Type { get; set; }
    public InvoiceDirection Direction { get; set; }

    // Alici
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxNumber { get; set; }
    public string? CustomerTaxOffice { get; set; }
    public string CustomerAddress { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }

    // Tutar
    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "TRY";

    public DateTime InvoiceDate { get; set; }

    public List<InvoiceLineDto> Lines { get; set; } = new();
}

public sealed class InvoiceLineDto
{
    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal? DiscountAmount { get; set; }
}
