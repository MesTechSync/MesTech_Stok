namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// MESA AI belge siniflandirma tamamlandiginda consume edilir.
/// Exchange: mestech.mesa.ai.document.classified.v1
/// </summary>
public record AiDocumentClassifiedEvent(
    Guid DocumentId,
    string DocumentType,
    decimal Confidence,
    decimal? ExtractedAmount,
    string? ExtractedVKN,
    Guid TenantId,
    DateTime OccurredAt);
