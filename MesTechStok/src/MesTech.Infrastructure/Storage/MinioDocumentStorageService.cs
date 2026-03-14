using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Storage;

/// <summary>
/// MinIO bazlı belge depolama servisi.
/// TODO: Minio.AspNetCore veya AWSSDK.S3 paketi eklenerek tam implement edilecek (Dalga 8 H27).
/// </summary>
public class MinioDocumentStorageService : IDocumentStorageService
{
    private readonly ILogger<MinioDocumentStorageService> _logger;

    public MinioDocumentStorageService(ILogger<MinioDocumentStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string bucket = "mestech-documents", CancellationToken cancellationToken = default)
    {
        // TODO: Dalga 8 H27'de MinIO SDK ile implement edilecek
        _logger.LogInformation("MinIO upload stub: {FileName} → {Bucket}", fileName, bucket);
        var path = $"{bucket}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{fileName}";
        return Task.FromResult(path);
    }

    public Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MinIO download stub: {Path}", storagePath);
        return Task.FromResult<Stream>(new MemoryStream());
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MinIO delete stub: {Path}", storagePath);
        return Task.CompletedTask;
    }

    public Task<string> GetPresignedUrlAsync(string storagePath, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MinIO presigned URL stub: {Path}", storagePath);
        return Task.FromResult($"http://localhost:9000/{storagePath}?stub=true&expiry={expiry.TotalSeconds}");
    }
}
