namespace MesTech.Application.Interfaces;

/// <summary>
/// Belge depolama servisi arayuzu — MinIO veya disk bazli implementasyon.
/// Infrastructure katmaninda implemente edilir.
/// </summary>
public interface IDocumentStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType,
        string bucket = "mestech-documents", CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    Task<string> GetPresignedUrlAsync(string storagePath, TimeSpan expiry,
        CancellationToken cancellationToken = default);
}
