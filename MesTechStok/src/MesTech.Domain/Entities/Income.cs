using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Gelir kaydı — OnMuhasebe modülü için.
/// </summary>
public class Income : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; set; } = "TRY";
    public IncomeType IncomeType { get; set; }
    public IncomeSource Source { get; set; } = IncomeSource.Manual;
    public Guid? OrderId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal CommissionAmount { get; private set; }
    public decimal ShippingCost { get; private set; }
    public decimal NetAmount => Amount - CommissionAmount - ShippingCost;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }

    public void SetAmount(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("Gelir tutarı negatif olamaz.", nameof(amount));
        Amount = amount;
    }

    public void SetDeductions(decimal commissionAmount, decimal shippingCost)
    {
        if (commissionAmount < 0) throw new ArgumentException("Komisyon negatif olamaz.", nameof(commissionAmount));
        if (shippingCost < 0) throw new ArgumentException("Kargo maliyeti negatif olamaz.", nameof(shippingCost));
        CommissionAmount = commissionAmount;
        ShippingCost = shippingCost;
    }
}
