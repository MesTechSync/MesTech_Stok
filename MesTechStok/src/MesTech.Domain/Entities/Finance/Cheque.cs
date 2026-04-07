using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Finance;

/// <summary>
/// Çek — alınan veya verilen çek kaydı.
/// Muhasebe: Alınan çek 101, verilen çek 103 hesap.
/// </summary>
public sealed class Cheque : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string ChequeNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime IssueDate { get; private set; }
    public DateTime MaturityDate { get; private set; }
    public string BankName { get; private set; } = string.Empty;
    public string? BranchCode { get; private set; }
    public ChequeType Type { get; private set; }
    public ChequeStatus Status { get; private set; }
    public string? DrawerName { get; private set; }
    public string? EndorserName { get; private set; }
    public Guid? RelatedInvoiceId { get; private set; }
    public string? Notes { get; private set; }

    private Cheque() { }

    public static Cheque Create(
        Guid tenantId, string chequeNumber, decimal amount,
        DateTime issueDate, DateTime maturityDate,
        string bankName, ChequeType type, string? drawerName = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(chequeNumber);
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Tutar pozitif olmali.");

        return new Cheque
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChequeNumber = chequeNumber,
            Amount = amount,
            IssueDate = issueDate,
            MaturityDate = maturityDate,
            BankName = bankName,
            Type = type,
            Status = ChequeStatus.InPortfolio,
            DrawerName = drawerName,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsOverdue => MaturityDate < DateTime.UtcNow && Status == ChequeStatus.InPortfolio;

    public void SendForCollection() { Status = ChequeStatus.SentForCollection; UpdatedAt = DateTime.UtcNow; }
    public void MarkCollected() { Status = ChequeStatus.Collected; UpdatedAt = DateTime.UtcNow; }
    public void MarkBounced() { Status = ChequeStatus.Bounced; UpdatedAt = DateTime.UtcNow; }
    public void Endorse(string endorserName) { EndorserName = endorserName; Status = ChequeStatus.Endorsed; UpdatedAt = DateTime.UtcNow; }
    public void Cancel() { Status = ChequeStatus.Cancelled; UpdatedAt = DateTime.UtcNow; }
}

public enum ChequeType { Received = 0, Given = 1 }
public enum ChequeStatus { InPortfolio = 0, SentForCollection = 1, Collected = 2, Bounced = 3, Endorsed = 4, Cancelled = 5 }
