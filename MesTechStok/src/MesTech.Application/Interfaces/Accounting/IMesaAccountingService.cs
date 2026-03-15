namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// MESA OS Muhasebe AI servis kontrati.
/// Belge siniflandirma, veri cikarma ve mutabakat onerisi saglar.
/// Dalga 1: MockMesaAccountingService (kural tabanli sahte veri).
/// Dalga 2+: RealMesaAccountingClient (HTTP -> MESA OS AI).
/// </summary>
public interface IMesaAccountingService
{
    /// <summary>Belge icerigini siniflandirir (fatura, fis, ekstre vb.).</summary>
    Task<DocumentClassification> ClassifyDocumentAsync(
        byte[] fileData,
        string mimeType,
        CancellationToken ct = default);

    /// <summary>Siniflandirilmis belgeden yapisal veri cikarir.</summary>
    Task<DocumentExtraction> ExtractDataAsync(
        byte[] fileData,
        DocumentClassification classification,
        CancellationToken ct = default);

    /// <summary>Settlement batch ile banka hareketleri arasinda mutabakat onerisi uretir.</summary>
    Task<ReconciliationSuggestion> SuggestReconciliationAsync(
        Guid settlementBatchId,
        IReadOnlyList<Guid> candidateBankTransactionIds,
        CancellationToken ct = default);
}

// -- Result Types --

/// <summary>Belge siniflandirma sonucu.</summary>
public record DocumentClassification(
    string DocumentType,
    decimal Confidence,
    string RawText);

/// <summary>Belgeden cikarilan yapisal veri.</summary>
public record DocumentExtraction(
    decimal? Amount,
    decimal? TaxAmount,
    string? CounterpartyName,
    string? VKN,
    DateTime? Date,
    Dictionary<string, string> ExtraFields);

/// <summary>Mutabakat onerisi sonucu.</summary>
public record ReconciliationSuggestion(
    Guid SettlementBatchId,
    Guid BankTransactionId,
    decimal Confidence,
    string Reason);
