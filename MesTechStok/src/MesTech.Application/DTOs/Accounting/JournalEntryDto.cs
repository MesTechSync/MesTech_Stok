namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Journal Entry data transfer object.
/// </summary>
public sealed class JournalEntryDto
{
    public Guid Id { get; set; }
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public bool IsPosted { get; set; }
    public DateTime? PostedAt { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public List<JournalLineDto> Lines { get; set; } = new();
}

public sealed class JournalLineDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string? AccountCode { get; set; }
    public string? AccountName { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}
