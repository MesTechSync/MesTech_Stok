using MesTech.Domain.Accounting.Events;
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
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        var netProfit = totalRevenue - totalCost - totalCommission - totalCargo - totalTax;

        var report = new ProfitReport
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

        report.RaiseDomainEvent(new ProfitReportGeneratedEvent
        {
            ReportId = report.Id,
            Period = period,
            Platform = platform,
            NetProfit = netProfit
        });

        return report;
    }
}
