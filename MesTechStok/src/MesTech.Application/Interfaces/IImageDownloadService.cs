namespace MesTech.Application.Interfaces;

/// <summary>
/// Resim indirme servisi — Polly retry, SemaphoreSlim concurrency limiti, SHA256 hash-based dedup.
/// ENT-DROP-IMP-SPRINT-D — DEV 3 Task D-06
/// </summary>
public interface IImageDownloadService
{
    /// <summary>
    /// URL listesini paralel indirir, hash-based dedup uygular.
    /// Başarısız URL'ler hata listesine eklenir, işlem devam eder.
    /// </summary>
    Task<ImageDownloadResult> DownloadBatchAsync(
        IEnumerable<string> imageUrls,
        ImageDownloadOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Tek URL indir — ön bellekte varsa döndür.
    /// </summary>
    Task<DownloadedImage?> DownloadSingleAsync(
        string imageUrl,
        CancellationToken ct = default);
}

/// <summary>Toplu indirme seçenekleri.</summary>
public record ImageDownloadOptions(
    int MaxConcurrency = 5,
    int MaxRetries = 3,
    TimeSpan Timeout = default,
    string? StoragePath = null,           // null = memory only
    bool DeduplicateByHash = true,
    long MaxFileSizeBytes = 5_242_880     // 5 MB
);

/// <summary>Toplu indirme sonucu.</summary>
public record ImageDownloadResult(
    IReadOnlyList<DownloadedImage> Succeeded,
    IReadOnlyList<ImageDownloadError> Failed,
    int DuplicatesSkipped
);

/// <summary>Başarıyla indirilen resim.</summary>
public record DownloadedImage(
    string OriginalUrl,
    string? LocalPath,
    IReadOnlyList<byte>? Data,
    string ContentType,
    string Sha256Hash,
    long SizeBytes
);

/// <summary>İndirme hatası detayı.</summary>
public record ImageDownloadError(
    string Url,
    string Reason,
    int HttpStatusCode
);
