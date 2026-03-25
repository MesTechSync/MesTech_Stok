using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Tekrarlayan gider — aylik/ceyreklik/yillik otomatik gider takibi.
/// </summary>
public sealed class RecurringExpense : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Frequency { get; set; } = "Monthly";  // Monthly, Quarterly, Annual
    public DateTime NextDueDate { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastProcessedAt { get; set; }
}
