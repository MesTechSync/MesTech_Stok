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
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public IncomeType IncomeType { get; set; }
    public IncomeSource Source { get; set; } = IncomeSource.Manual;
    public Guid? OrderId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal NetAmount => Amount - CommissionAmount - ShippingCost;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}
