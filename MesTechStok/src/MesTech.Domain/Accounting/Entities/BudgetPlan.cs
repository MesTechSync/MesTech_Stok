using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Butce plani — planlanan vs gerceklesen gelir/gider karsilastirmasi.
/// </summary>
public sealed class BudgetPlan : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal PlannedRevenue { get; set; }
    public decimal PlannedExpense { get; set; }
    public decimal ActualRevenue { get; set; }
    public decimal ActualExpense { get; set; }
    public decimal Variance { get => (ActualRevenue - ActualExpense) - (PlannedRevenue - PlannedExpense); private set { } }
    public string Status { get; set; } = "Draft";  // Draft, Active, Closed
}
