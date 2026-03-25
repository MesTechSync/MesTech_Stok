using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Common;
using MesTech.Domain.Exceptions;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Yevmiye kaydi — muhasebe fisinin baslik kismi.
/// Borç = Alacak kurali Validate() ile garanti altina alinir.
/// </summary>
public sealed class JournalEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DateTime EntryDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ReferenceNumber { get; private set; }
    public bool IsPosted { get; private set; }
    public DateTime? PostedAt { get; private set; }

    private readonly List<JournalLine> _lines = new();
    public IReadOnlyList<JournalLine> Lines => _lines.AsReadOnly();

    private JournalEntry() { }

    public static JournalEntry Create(
        Guid tenantId,
        DateTime entryDate,
        string description,
        string? referenceNumber = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));

        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntryDate = entryDate,
            Description = description,
            ReferenceNumber = referenceNumber,
            IsPosted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddLine(Guid accountId, decimal debit, decimal credit, string? description = null)
    {
        if (IsPosted)
            throw new InvalidOperationException("Cannot modify a posted journal entry.");

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account ID cannot be empty.", nameof(accountId));

        if (debit < 0 || credit < 0)
            throw new ArgumentException("Debit and credit amounts must be non-negative.", nameof(debit));

        if (debit > 0 && credit > 0)
            throw new ArgumentException("A journal line cannot have both debit and credit.", nameof(credit));

        if (debit == 0 && credit == 0)
            throw new ArgumentException("Either debit or credit must be greater than zero.", nameof(debit));

        var line = JournalLine.Create(Id, accountId, debit, credit, description);
        _lines.Add(line);
    }

    /// <summary>
    /// Borc = Alacak kuralini dogrular.
    /// Esit degilse JournalEntryImbalanceException firlatir.
    /// </summary>
    public void Validate()
    {
        var totalDebit = _lines.Sum(l => l.Debit);
        var totalCredit = _lines.Sum(l => l.Credit);

        if (totalDebit != totalCredit)
            throw new JournalEntryImbalanceException(totalDebit, totalCredit);

        if (_lines.Count < 2)
            throw new InvalidOperationException("A journal entry must have at least 2 lines.");
    }

    public void Post()
    {
        if (IsPosted)
            throw new InvalidOperationException("Journal entry is already posted.");

        Validate();

        IsPosted = true;
        PostedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new LedgerPostedEvent
        {
            TenantId = TenantId,
            JournalEntryId = Id,
            EntryDate = EntryDate,
            TotalAmount = _lines.Sum(l => l.Debit),
            LineCount = _lines.Count
        });
    }
}

