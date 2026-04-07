using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Finance;

/// <summary>
/// Senet — alınan veya verilen senet kaydı.
/// Muhasebe: Alınan senet 121, verilen senet 321 hesap.
/// </summary>
public sealed class PromissoryNote : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string NoteNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime IssueDate { get; private set; }
    public DateTime MaturityDate { get; private set; }
    public NoteType Type { get; private set; }
    public NoteStatus Status { get; private set; }
    public string DebtorName { get; private set; } = string.Empty;
    public Guid? RelatedInvoiceId { get; private set; }
    public string? Notes { get; private set; }

    private PromissoryNote() { }

    public static PromissoryNote Create(
        Guid tenantId, string noteNumber, decimal amount,
        DateTime issueDate, DateTime maturityDate,
        NoteType type, string debtorName)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(noteNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(debtorName);
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        return new PromissoryNote
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NoteNumber = noteNumber,
            Amount = amount,
            IssueDate = issueDate,
            MaturityDate = maturityDate,
            Type = type,
            Status = NoteStatus.InPortfolio,
            DebtorName = debtorName,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsOverdue => MaturityDate < DateTime.UtcNow && Status == NoteStatus.InPortfolio;

    public void MarkCollected() { Status = NoteStatus.Collected; UpdatedAt = DateTime.UtcNow; }
    public void MarkProtested() { Status = NoteStatus.Protested; UpdatedAt = DateTime.UtcNow; }
    public void Cancel() { Status = NoteStatus.Cancelled; UpdatedAt = DateTime.UtcNow; }
}

public enum NoteType { Received = 0, Given = 1 }
public enum NoteStatus { InPortfolio = 0, SentForCollection = 1, Collected = 2, Protested = 3, Cancelled = 4 }
