using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class InvoiceLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal? DiscountAmount { get; set; }

    public Invoice? Invoice { get; set; }
    public Product? Product { get; set; }

    public void CalculateLineTotal()
    {
        var subtotal = UnitPrice * Quantity - (DiscountAmount ?? 0);
        TaxAmount = subtotal * TaxRate;
        LineTotal = subtotal + TaxAmount;
    }
}
