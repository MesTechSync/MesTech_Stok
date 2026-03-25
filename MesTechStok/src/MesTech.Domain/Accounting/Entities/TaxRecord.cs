using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Vergi kaydi — donem bazinda vergi tutarlari ve odeme durumu.
/// </summary>
public sealed class TaxRecord : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Period { get; private set; } = string.Empty;
    public string TaxType { get; private set; } = string.Empty;
    public decimal TaxableAmount { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public int? Year { get; private set; }
    public DateTime DueDate { get; private set; }
    public bool IsPaid { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public decimal PenaltyAmount { get; private set; }

    private TaxRecord() { }

    public static TaxRecord Create(
        Guid tenantId,
        string period,
        string taxType,
        decimal taxableAmount,
        decimal taxAmount,
        DateTime dueDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(period);
        ArgumentException.ThrowIfNullOrWhiteSpace(taxType);

        return new TaxRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Period = period,
            TaxType = taxType,
            TaxableAmount = taxableAmount,
            TaxAmount = taxAmount,
            DueDate = dueDate,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsPaid()
    {
        IsPaid = true;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
