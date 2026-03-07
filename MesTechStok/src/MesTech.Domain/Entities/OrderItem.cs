using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Sipariş kalemi.
/// </summary>
public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }

    // ── Domain Logic ──

    public decimal SubTotal => Quantity * UnitPrice;

    public void CalculateAmounts()
    {
        TotalPrice = Quantity * UnitPrice;
        TaxAmount = TotalPrice * TaxRate;
    }

    public override string ToString() => $"{ProductName} x{Quantity} = {TotalPrice:C}";
}
