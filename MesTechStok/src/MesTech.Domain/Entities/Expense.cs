using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Gider kaydı — OnMuhasebe modülü için.
/// </summary>
public class Expense : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public ExpenseType ExpenseType { get; set; }
    public ExpenseCategory Category { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? InvoiceNumber { get; set; }
    public Guid? SupplierId { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime? DueDate { get; set; }
    public string? Note { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePeriod { get; set; }
}
