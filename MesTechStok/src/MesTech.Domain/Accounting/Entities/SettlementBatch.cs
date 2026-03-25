using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Platform hesap kesimi — Trendyol, Hepsiburada vb. periyodik odeme toplami.
/// </summary>
public sealed class SettlementBatch : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Platform { get; private set; } = string.Empty;
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public decimal TotalGross { get; private set; }
    public decimal TotalCommission { get; private set; }
    public decimal TotalNet { get; private set; }
    public SettlementStatus Status { get; private set; }
    public DateTime ImportedAt { get; private set; }

    private readonly List<SettlementLine> _lines = new();
    public IReadOnlyList<SettlementLine> Lines => _lines.AsReadOnly();

    private SettlementBatch() { }

    public static SettlementBatch Create(
        Guid tenantId,
        string platform,
        DateTime periodStart,
        DateTime periodEnd,
        decimal totalGross,
        decimal totalCommission,
        decimal totalNet)
    {
        // Note: tenantId may be Guid.Empty when created by settlement parsers
        // (infrastructure layer). The caller/command handler sets the real tenant ID later.

        ArgumentException.ThrowIfNullOrWhiteSpace(platform);

        if (periodEnd < periodStart)
            throw new ArgumentException("Period end cannot be before period start.", nameof(periodEnd));

        var batch = new SettlementBatch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Platform = platform,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalGross = totalGross,
            TotalCommission = totalCommission,
            TotalNet = totalNet,
            Status = SettlementStatus.Imported,
            ImportedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        batch.RaiseDomainEvent(new SettlementImportedEvent
        {
            TenantId = tenantId,
            SettlementBatchId = batch.Id,
            Platform = platform,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalNet = totalNet
        });

        return batch;
    }

    public void AddLine(SettlementLine line)
    {
        _lines.Add(line);
    }

    public void MarkReconciled()
    {
        Status = SettlementStatus.Reconciled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDisputed()
    {
        Status = SettlementStatus.Disputed;
        UpdatedAt = DateTime.UtcNow;
    }
}
