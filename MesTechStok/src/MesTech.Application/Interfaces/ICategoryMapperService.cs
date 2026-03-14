namespace MesTech.Application.Interfaces;

/// <summary>
/// Tedarikçi kategori yolunu MesTech kategori ID'sine eşleştirir.
/// Fuzzy match: Levenshtein mesafesi + keyword overlap, 24h cache.
/// ENT-DROP-IMP-SPRINT-D — DEV 1 Task D-02
/// </summary>
public interface ICategoryMapperService
{
    /// <summary>
    /// Tedarikçi kategori yolunu MesTech kategori ID'sine eşleştirir.
    /// Kesin eşleşme bulamazsa null döner.
    /// </summary>
    Task<CategoryMapResult?> MapAsync(
        string supplierCategoryPath,
        string? platformCode = null,
        CancellationToken ct = default);

    /// <summary>
    /// Manuel mapping kaydet (kullanıcı override).
    /// Bir sonraki çağrıda cache'den gelir.
    /// </summary>
    Task SaveManualMappingAsync(
        string supplierCategoryPath,
        Guid targetCategoryId,
        CancellationToken ct = default);
}

public record CategoryMapResult(
    Guid CategoryId,
    string CategoryPath,
    decimal Confidence,       // 0.0–1.0
    bool IsManual,            // Kullanıcı tarafından kaydedilmiş
    bool IsExact              // Birebir eşleşme
);
