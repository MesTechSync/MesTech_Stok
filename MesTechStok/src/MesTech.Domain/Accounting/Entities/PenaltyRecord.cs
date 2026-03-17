using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Ceza kaydi — platform veya resmi kurum kaynakli cezalar.
/// </summary>
public class PenaltyRecord : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public PenaltySource Source { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public DateTime PenaltyDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public Guid? RelatedOrderId { get; private set; }
    public string? Notes { get; private set; }

    private PenaltyRecord() { }

    public static PenaltyRecord Create(
        Guid tenantId,
        PenaltySource source,
        string description,
        decimal amount,
        DateTime penaltyDate,
        DateTime? dueDate = null,
        string? referenceNumber = null,
        Guid? relatedOrderId = null,
        string currency = "TRY",
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");

        return new PenaltyRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Source = source,
            Description = description,
            Amount = amount,
            Currency = currency,
            PenaltyDate = penaltyDate,
            DueDate = dueDate,
            PaymentStatus = PaymentStatus.Pending,
            ReferenceNumber = referenceNumber,
            RelatedOrderId = relatedOrderId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsPaid()
    {
        PaymentStatus = PaymentStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePaymentStatus(PaymentStatus status)
    {
        PaymentStatus = status;
        UpdatedAt = DateTime.UtcNow;
    }
}
