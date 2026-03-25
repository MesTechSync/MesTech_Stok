using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Kar/zarar girisi — donem bazli gelir/gider takibi.
/// </summary>
public class ProfitLossEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Period { get; set; } = string.Empty;  // "2026-03" format
    public decimal RevenueAmount { get; set; }
    public decimal ExpenseAmount { get; set; }
    public decimal NetProfit { get => RevenueAmount - ExpenseAmount; private set { } }
    public string Category { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CalculatedAt { get; set; }
}
