namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// MESA AI belge icerik cikarimi tamamlandiginda consume edilir.
/// Exchange: mestech.mesa.ai.document.extracted.v1
/// </summary>
public record AiDocumentExtractedEvent(
    Guid DocumentId,
    string ProcessedJson,
    decimal Confidence,
    decimal? ExtractedAmount,
    string? ExtractedVKN,
    string? ExtractedCategory,
    Guid TenantId,
    DateTime OccurredAt);
