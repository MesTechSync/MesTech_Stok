using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Reporting;

/// <summary>
/// Rapor tanimi — zamanlanmis veya talep usulu rapor sablonu.
/// FilterJson ile tarih araligi, platform, kategori filtresi saklanir.
/// </summary>
public sealed class ReportDefinition : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public ReportType Type { get; private set; }
    public ReportFrequency Frequency { get; private set; }
    public string? FilterJson { get; private set; }
    public string? RecipientEmail { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }

    private ReportDefinition() { }

    public static ReportDefinition Create(
        Guid tenantId, string name, ReportType type,
        ReportFrequency frequency = ReportFrequency.OnDemand,
        string? recipientEmail = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ReportDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Type = type,
            Frequency = frequency,
            RecipientEmail = recipientEmail,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetFilter(string filterJson) { FilterJson = filterJson; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}

public enum ReportType
{
    SalesByPlatform, SalesByCategory, SalesByProduct,
    StockStatus, StockMovement, StockABC,
    OrderStatus, OrderByPlatform, ReturnAnalysis,
    ProfitLoss, CommissionComparison, CashFlow,
    CustomerSegment, CustomerLifetimeValue,
    CargoPerformance, DeliveryTime
}

public enum ReportFrequency
{
    OnDemand = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}
