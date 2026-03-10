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
    public IncomeType IncomeType { get; set; }
    public Guid? InvoiceId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}
