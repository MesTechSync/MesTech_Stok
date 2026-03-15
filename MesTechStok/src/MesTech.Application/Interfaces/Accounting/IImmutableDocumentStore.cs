namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// VUK 253 uyumlu degistirilemez belge deposu.
/// 5 yillik zorunlu saklama — DELETE metodu YOKTUR (compile-time garanti).
/// </summary>
public interface IImmutableDocumentStore
{
    /// <summary>
    /// Belgeyi arsive kaydeder ve SHA-256 hash ile butunluk garantisi saglar.
    /// </summary>
    Task<Guid> StoreAsync(byte[] content, string mimeType, DocumentMetadata metadata, CancellationToken ct = default);

    /// <summary>
    /// Arsivlenmis belgeyi ve meta verisini getirir.
    /// </summary>
    Task<(byte[] Content, DocumentMetadata Metadata)> RetrieveAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Belgenin butunlugunu dogrular — SHA-256 hash karsilastirir.
    /// </summary>
    Task<bool> VerifyIntegrityAsync(Guid documentId, CancellationToken ct = default);

    // NO delete method — VUK 253: 5-year mandatory retention
}

/// <summary>
/// Arsivlenmis belge meta verisi — WORM (Write Once Read Many) uyumlu.
/// </summary>
public record DocumentMetadata(
    string SourceHash,
    DateTime ArchivedAt,
    string SourceChannel,
    string? UblTrVersion,
    string? SchematronVersion,
    Guid TenantId,
    Guid? OriginalDocumentId
);
