using MesTech.Domain.Accounting.Enums;

namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Muhasebe belgesi alindiginda tetiklenir.
/// </summary>
public record DocumentReceivedEvent : AccountingDomainEvent
{
    public Guid DocumentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }
    public DocumentSource Source { get; init; }
}
