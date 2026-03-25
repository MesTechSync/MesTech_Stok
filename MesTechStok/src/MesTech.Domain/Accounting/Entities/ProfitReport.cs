using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Kar/zarar raporu — donem ve platform bazinda finansal ozet.
/// </summary>
public sealed class ProfitReport : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DateTime ReportDate { get; private set; }
    public string? Platform { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public decimal TotalCost { get; private set; }
    public decimal TotalCommission { get; private set; }
    public decimal TotalCargo { get; private set; }
    public decimal TotalTax { get; private set; }
    public decimal NetProfit { get; private set; }
    public string Period { get; private set; } = string.Empty;

    private ProfitReport() { }

    public static ProfitReport Create(
        Guid tenantId,
        DateTime reportDate,
        string period,
        decimal totalRevenue,
        decimal totalCost,
        decimal totalCommission,
        decimal totalCargo,
        decimal totalTax,
        string? platform = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        var netProfit = totalRevenue - totalCost - totalCommission - totalCargo - totalTax;

        return new ProfitReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportDate = reportDate,
            Platform = platform,
            TotalRevenue = totalRevenue,
            TotalCost = totalCost,
            TotalCommission = totalCommission,
            TotalCargo = totalCargo,
            TotalTax = totalTax,
            NetProfit = netProfit,
            Period = period,
            CreatedAt = DateTime.UtcNow
        };
    }
}
