using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace MesTech.Infrastructure.Storage;

/// <summary>
/// MinIO bazli belge depolama servisi.
/// Dalga 8 H27: Minio SDK 6.0.3 ile gercek implementasyon.
/// </summary>
public sealed class MinioDocumentStorageService : Application.Interfaces.IDocumentStorageService, IDocumentStorageService
{
    private readonly IMinioClient _minio;
    private readonly string _endpoint;
    private readonly ILogger<MinioDocumentStorageService> _logger;

    public MinioDocumentStorageService(
        IMinioClient minio,
        IConfiguration config,
        ILogger<MinioDocumentStorageService> logger)
    {
        _minio = minio;
        _endpoint = config["MinIO:Endpoint"] ?? "localhost:3900";
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream fileStream, string fileName, string contentType,
        string bucket = "mestech-documents",
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(bucket, cancellationToken).ConfigureAwait(false);

        var objectName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{fileName}";

        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType), cancellationToken)
            .ConfigureAwait(false);

        var storagePath = $"{bucket}/{objectName}";
        _logger.LogInformation("MinIO upload: {Path}", storagePath);
        return storagePath;
    }

    /// <summary>
    /// Downloads file from MinIO. Caller MUST dispose the returned stream.
    /// Usage: await using var stream = await DownloadAsync(path, ct);
    /// </summary>
    public async Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var (bucket, obj) = ParsePath(storagePath);
        var ms = new MemoryStream();

        try
        {
            await _minio.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(obj)
                .WithCallbackStream(s => s.CopyTo(ms)), cancellationToken)
                .ConfigureAwait(false);

            ms.Position = 0;
            _logger.LogInformation("MinIO download: {Path}", storagePath);
            return ms;
        }
        catch
        {
            await ms.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var (bucket, obj) = ParsePath(storagePath);
        await _minio.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(bucket)
            .WithObject(obj), cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("MinIO delete: {Path}", storagePath);
    }

    public async Task<string> GetPresignedUrlAsync(
        string storagePath, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var (bucket, obj) = ParsePath(storagePath);
        var url = await _minio.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(obj)
            .WithExpiry((int)expiry.TotalSeconds))
            .ConfigureAwait(false);

        _logger.LogInformation("MinIO presigned URL generated for: {Path}", storagePath);
        return url;
    }

    // ── Yardimci ──

    private async Task EnsureBucketExistsAsync(string bucket, CancellationToken ct)
    {
        var exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucket), ct)
            .ConfigureAwait(false);

        if (!exists)
        {
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucket), ct)
                .ConfigureAwait(false);

            _logger.LogInformation("MinIO bucket created: {Bucket}", bucket);
        }
    }

    private static (string bucket, string obj) ParsePath(string path)
    {
        var idx = path.IndexOf('/', StringComparison.Ordinal);
        if (idx < 0)
            throw new ArgumentException($"Gecersiz storage path: {path}");
        return (path[..idx], path[(idx + 1)..]);
    }
}
