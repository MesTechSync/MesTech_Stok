using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// İade talebindeki ürün kalemi.
/// </summary>
public class ReturnRequestLine : BaseEntity
{
    public Guid ReturnRequestId { get; set; }
    public Guid? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }

    // Navigation
    public ReturnRequest? ReturnRequest { get; set; }
    public Product? Product { get; set; }

    public void CalculateRefund()
    {
        RefundAmount = UnitPrice * Quantity;
    }

    public override string ToString() => $"{ProductName} x{Quantity} = {RefundAmount:C}";
}
