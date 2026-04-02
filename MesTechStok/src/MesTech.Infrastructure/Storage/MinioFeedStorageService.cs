using System.Text;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Feed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Infrastructure.Storage;

/// <summary>
/// MinIO bazlı feed depolama servisi.
/// Feed XML'lerini mestech-feeds bucket'ına yükler.
/// Path format: {platform}/{storeId}.xml
/// G10760: Feed XML üretiliyor ama persist edilmiyordu — bu servis eksik parçayı tamamlar.
/// </summary>
public sealed class MinioFeedStorageService : IFeedStorageService
{
    private const string FeedBucket = "mestech-feeds";

    private readonly IDocumentStorageService _storage;
    private readonly FeedOptions _feedOptions;
    private readonly ILogger<MinioFeedStorageService> _logger;

    public MinioFeedStorageService(
        IDocumentStorageService storage,
        IOptions<FeedOptions> feedOptions,
        ILogger<MinioFeedStorageService> logger)
    {
        _storage = storage;
        _feedOptions = feedOptions?.Value ?? new FeedOptions();
        _logger = logger;
    }

    public async Task<string> UploadFeedAsync(
        string platform, Guid storeId, string xmlContent, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlContent);

        var objectName = $"{platform}/{storeId:N}.xml";
        var bytes = Encoding.UTF8.GetBytes(xmlContent);
        using var stream = new MemoryStream(bytes);

        await _storage.UploadAsync(
            stream,
            objectName,
            "application/xml",
            FeedBucket,
            ct).ConfigureAwait(false);

        var feedUrl = $"{_feedOptions.FeedBaseUrl.TrimEnd('/')}/{objectName}";

        _logger.LogInformation(
            "[FeedStorage] Uploaded {Platform} feed for store {StoreId} ({Size} bytes) → {Url}",
            platform, storeId, bytes.Length, feedUrl);

        return feedUrl;
    }

    public async Task<string?> DownloadFeedAsync(
        string platform, Guid storeId, CancellationToken ct = default)
    {
        var storagePath = $"{FeedBucket}/{platform}/{storeId:N}.xml";

        try
        {
            await using var stream = await _storage.DownloadAsync(storagePath, ct).ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FeedStorage] Feed not found: {Path}", storagePath);
            return null;
        }
    }

    public async Task DeleteFeedAsync(
        string platform, Guid storeId, CancellationToken ct = default)
    {
        var storagePath = $"{FeedBucket}/{platform}/{storeId:N}.xml";
        await _storage.DeleteAsync(storagePath, ct).ConfigureAwait(false);
        _logger.LogInformation("[FeedStorage] Deleted feed: {Path}", storagePath);
    }
}
