using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Mutabakat eslestirme kaydi — banka hareketi ile hesap kesimi arasi eslestirme.
/// </summary>
public sealed class ReconciliationMatch : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? SettlementBatchId { get; private set; }
    public Guid? BankTransactionId { get; private set; }
    public DateTime MatchDate { get; private set; }
    public decimal Confidence { get; private set; }
    public ReconciliationStatus Status { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public byte[]? RowVersion { get; set; }

    private ReconciliationMatch() { }

    public static ReconciliationMatch Create(
        Guid tenantId,
        DateTime matchDate,
        decimal confidence,
        ReconciliationStatus status,
        Guid? settlementBatchId = null,
        Guid? bankTransactionId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));

        if (confidence < 0 || confidence > 1)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");

        var match = new ReconciliationMatch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SettlementBatchId = settlementBatchId,
            BankTransactionId = bankTransactionId,
            MatchDate = matchDate,
            Confidence = confidence,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        if (status == ReconciliationStatus.AutoMatched)
        {
            match.RaiseDomainEvent(new ReconciliationMatchedEvent
            {
                TenantId = tenantId,
                ReconciliationMatchId = match.Id,
                BankTransactionId = bankTransactionId,
                SettlementBatchId = settlementBatchId,
                Confidence = confidence
            });
        }

        return match;
    }

    public void Approve(string reviewedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);
        if (Status is ReconciliationStatus.ManualMatch or ReconciliationStatus.Rejected)
            throw new InvalidOperationException(
                $"Cannot approve reconciliation in {Status} status. Only AutoMatched or NeedsReview can be approved.");
        Status = ReconciliationStatus.ManualMatch;
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReconciliationCompletedEvent
        {
            MatchId = Id,
            SettlementBatchId = SettlementBatchId,
            BankTransactionId = BankTransactionId,
            FinalStatus = ReconciliationStatus.ManualMatch,
            Confidence = Confidence
        });
    }

    public void Reject(string reviewedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);
        if (Status is ReconciliationStatus.ManualMatch or ReconciliationStatus.Rejected)
            throw new InvalidOperationException(
                $"Cannot reject reconciliation in {Status} status. Only AutoMatched or NeedsReview can be rejected.");
        Status = ReconciliationStatus.Rejected;
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReconciliationCompletedEvent
        {
            MatchId = Id,
            SettlementBatchId = SettlementBatchId,
            BankTransactionId = BankTransactionId,
            FinalStatus = ReconciliationStatus.Rejected,
            Confidence = Confidence
        });
    }
}
