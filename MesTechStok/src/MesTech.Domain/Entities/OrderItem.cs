using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Sipariş kalemi.
/// </summary>
public sealed class OrderItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public int Quantity { get; internal set; }
    public decimal UnitPrice { get; internal set; }
    public decimal TotalPrice { get; internal set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; internal set; }

    // ── Domain Logic ──

    public decimal SubTotal => Quantity * UnitPrice;

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
        CalculateAmounts();
    }

    public void CalculateAmounts()
    {
        TotalPrice = Math.Round(Quantity * UnitPrice, 2);
        TaxAmount = Math.Round(TotalPrice * TaxRate, 2);
    }

    public override string ToString() => $"{ProductName} x{Quantity} = {TotalPrice:C}";
}
