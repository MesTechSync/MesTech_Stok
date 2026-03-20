using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Butce plani — planlanan vs gerceklesen gelir/gider karsilastirmasi.
/// </summary>
public class BudgetPlan : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal PlannedRevenue { get; set; }
    public decimal PlannedExpense { get; set; }
    public decimal ActualRevenue { get; set; }
    public decimal ActualExpense { get; set; }
    public decimal Variance => (ActualRevenue - ActualExpense) - (PlannedRevenue - PlannedExpense);
    public string Status { get; set; } = "Draft";  // Draft, Active, Closed
}
