using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Reporting;

/// <summary>
/// KPI snapshot — gunluk/haftalik metrik kaydi.
/// Dashboard widget'lari ve trend grafikleri bu entity'den beslenir.
/// </summary>
public sealed class KpiSnapshot : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DateTime SnapshotDate { get; private set; }
    public KpiType Type { get; private set; }
    public decimal Value { get; private set; }
    public decimal? PreviousValue { get; private set; }
    public string? PlatformCode { get; private set; }

    public decimal ChangePercent => PreviousValue.HasValue && PreviousValue.Value > 0
        ? ((Value - PreviousValue.Value) / PreviousValue.Value) * 100m
        : 0m;

    private KpiSnapshot() { }

    public static KpiSnapshot Create(
        Guid tenantId, DateTime snapshotDate, KpiType type, decimal value,
        decimal? previousValue = null, string? platformCode = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));

        return new KpiSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SnapshotDate = snapshotDate,
            Type = type,
            Value = value,
            PreviousValue = previousValue,
            PlatformCode = platformCode,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum KpiType
{
    TotalRevenue, TotalOrders, TotalReturns, ReturnRate,
    AverageOrderValue, GrossMargin, NetMargin,
    ActiveProducts, OutOfStock, LowStock,
    PendingShipments, DeliveredToday,
    NewCustomers, RepeatCustomers,
    CommissionTotal, SettlementPending
}
