using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public sealed class QuotationLine : BaseEntity
{
    public Guid QuotationId { get; set; }
    public Guid? ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public string? Description { get; set; }

    // Computed properties — not stored
    public decimal LineTotal => Quantity * UnitPrice;
    public decimal TaxAmount => Quantity * UnitPrice * TaxRate / 100;

    // Navigation
    public Quotation? Quotation { get; set; }
    public Product? Product { get; set; }
}
