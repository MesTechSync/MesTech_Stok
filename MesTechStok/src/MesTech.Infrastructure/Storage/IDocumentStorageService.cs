namespace MesTech.Infrastructure.Storage;

/// <summary>
/// Belge depolama servisi arayüzü — MinIO veya disk bazlı implementasyon
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>Dosya yükle, storage path döner.</summary>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string bucket = "mestech-documents", CancellationToken cancellationToken = default);

    /// <summary>Dosya indir.</summary>
    Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>Dosya sil.</summary>
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>Geçici erişim URL'i üret (presigned URL).</summary>
    Task<string> GetPresignedUrlAsync(string storagePath, TimeSpan expiry, CancellationToken cancellationToken = default);
}
