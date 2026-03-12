using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Bitrix24 Deal ürün satırı — crm.deal.productrows.set ile gönderilen kalemler.
/// </summary>
public class Bitrix24DealProductRow : BaseEntity
{
    public Guid Bitrix24DealId { get; set; }
    public Guid? ProductId { get; set; }
    public string ExternalProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Discount { get; set; }

    // Computed — EF Core should Ignore these
    public decimal LineTotal => Quantity * UnitPrice * (1 - Discount / 100);
    public decimal TaxAmount => Quantity * UnitPrice * TaxRate / 100;

    // Navigation
    public Bitrix24Deal Deal { get; set; } = null!;
    public Product? Product { get; set; }
}
