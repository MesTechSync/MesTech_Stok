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
    public int Quantity { get; internal set; }
    public decimal UnitPrice { get; internal set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; internal set; }
    public decimal LineTotal { get; internal set; }
    public decimal? DiscountAmount { get; set; }

    public Invoice? Invoice { get; set; }
    public Product? Product { get; set; }

    public void SetQuantityAndPrice(int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Miktar pozitif olmalı.", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("Birim fiyat negatif olamaz.", nameof(unitPrice));

        Quantity = quantity;
        UnitPrice = unitPrice;
        CalculateLineTotal();
    }

    public void CalculateLineTotal()
    {
        var subtotal = UnitPrice * Quantity - (DiscountAmount ?? 0);
        TaxAmount = subtotal * TaxRate;
        LineTotal = subtotal + TaxAmount;
    }
}
