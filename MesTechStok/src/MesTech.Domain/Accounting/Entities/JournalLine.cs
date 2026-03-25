using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Yevmiye satiri — bir hesaba borc veya alacak kaydi.
/// JournalEntry'nin alt kaydi.
/// </summary>
public sealed class JournalLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid JournalEntryId { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public string? Description { get; private set; }

    // Navigation
    public JournalEntry? JournalEntry { get; private set; }
    public ChartOfAccounts? Account { get; private set; }

    private JournalLine() { }

    public static JournalLine Create(
        Guid journalEntryId,
        Guid accountId,
        decimal debit,
        decimal credit,
        string? description = null)
    {
        return new JournalLine
        {
            Id = Guid.NewGuid(),
            JournalEntryId = journalEntryId,
            AccountId = accountId,
            Debit = debit,
            Credit = credit,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}
