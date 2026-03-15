namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// Muhasebe belgesi alindiginda publish edilir.
/// Exchange: mestech.mesa.finance.document.received.v1
/// </summary>
public record FinanceDocumentReceivedEvent(
    Guid DocumentId,
    string FileName,
    string MimeType,
    string Source,
    Guid TenantId,
    DateTime OccurredAt);
