using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public sealed class InvoiceLine : BaseEntity, ITenantEntity
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
        if (TaxRate < 0 || TaxRate > 1)
            throw new InvalidOperationException($"Vergi oranı 0 ile 1 arasında olmalı. Mevcut: {TaxRate}");

        Quantity = quantity;
        UnitPrice = unitPrice;
        CalculateLineTotal();
    }

    public void CalculateLineTotal()
    {
        var discount = DiscountAmount ?? 0;
        if (discount < 0)
            throw new InvalidOperationException($"İndirim tutarı negatif olamaz. Mevcut: {discount}");

        var grossAmount = UnitPrice * Quantity;
        if (discount > grossAmount)
            throw new InvalidOperationException(
                $"İndirim tutarı ({discount:C}) brüt tutarı ({grossAmount:C}) aşamaz.");

        var subtotal = grossAmount - discount;
        TaxAmount = Math.Round(subtotal * TaxRate, 2);
        LineTotal = Math.Round(subtotal + TaxAmount, 2);
    }
}
