using System.Collections.Concurrent;
using System.Security.Cryptography;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Security;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Dropshipping;

/// <summary>
/// Resim indirme servisi implementasyonu.
/// Polly 8.x ResiliencePipeline ile retry (exponential backoff),
/// SemaphoreSlim ile eşzamanlılık sınırı, SHA256 ile hash-based dedup.
/// ENT-DROP-IMP-SPRINT-D — DEV 3 Task D-06
/// </summary>
public sealed class ImageDownloadService(
    IHttpClientFactory httpClientFactory,
    ILogger<ImageDownloadService> logger
) : IImageDownloadService
{
    private readonly ConcurrentDictionary<string, DownloadedImage> _hashCache = new();

    public async Task<ImageDownloadResult> DownloadBatchAsync(
        IEnumerable<string> imageUrls,
        ImageDownloadOptions options,
        CancellationToken ct = default)
    {
        var urls = imageUrls.Distinct().ToList();
        using var semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);
        var succeeded  = new ConcurrentBag<DownloadedImage>();
        var failed     = new ConcurrentBag<ImageDownloadError>();
        int duplicates = 0;

        var tasks = urls.Select(async url =>
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var image = await DownloadWithPolicyAsync(url, options, ct).ConfigureAwait(false);
                if (image is null) return;

                // Hash-based dedup (ConcurrentDictionary — thread-safe)
                if (options.DeduplicateByHash && !_hashCache.TryAdd(image.Sha256Hash, image))
                {
                    Interlocked.Increment(ref duplicates);
                    logger.LogDebug("Duplicate image atlandı: {Hash}", image.Sha256Hash[..8]);
                    return;
                }

                // Diske kaydet (opsiyonel)
                if (!string.IsNullOrEmpty(options.StoragePath))
                    await SaveToDiskAsync(image, options.StoragePath, ct).ConfigureAwait(false);

                succeeded.Add(image);
            }
            catch (Exception ex)
            {
                failed.Add(new ImageDownloadError(url, ex.Message, 0));
                logger.LogWarning("Resim indirilemedi: {Url} — {Error}", url, ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        logger.LogInformation(
            "Batch download: {Success} başarılı, {Failed} hata, {Dup} duplicate atlandı",
            succeeded.Count, failed.Count, duplicates);

        return new ImageDownloadResult(
            succeeded.ToList(), failed.ToList(), duplicates);
    }

    public async Task<DownloadedImage?> DownloadSingleAsync(
        string imageUrl, CancellationToken ct = default)
    {
        var opts = new ImageDownloadOptions();
        return await DownloadWithPolicyAsync(imageUrl, opts, ct).ConfigureAwait(false);
    }

    private async Task<DownloadedImage?> DownloadWithPolicyAsync(
        string url, ImageDownloadOptions options, CancellationToken ct)
    {
        if (!SsrfGuard.ValidateUrl(url, logger, nameof(ImageDownloadService)))
            return null;

        var timeout = options.Timeout == default
            ? TimeSpan.FromSeconds(30)
            : options.Timeout;

        // Polly 8.x: ResiliencePipeline<DownloadedImage?> ile retry (exponential backoff)
        var pipeline = new ResiliencePipelineBuilder<DownloadedImage?>()
            .AddRetry(new RetryStrategyOptions<DownloadedImage?>
            {
                MaxRetryAttempts = options.MaxRetries,
                BackoffType      = DelayBackoffType.Exponential,
                Delay            = TimeSpan.FromSeconds(1),
                ShouldHandle     = new PredicateBuilder<DownloadedImage?>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    logger.LogDebug(
                        "Resim retry {Attempt}/{Max}: {Url} ({Error})",
                        args.AttemptNumber + 1, options.MaxRetries, url,
                        args.Outcome.Exception?.Message ?? "bilinmeyen hata");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        return await pipeline.ExecuteAsync(async cancelToken =>
        {
            var client = httpClientFactory.CreateClient("ImageDownloader");
            client.Timeout = timeout;

            using var response = await client
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancelToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Resim HTTP {Status}: {Url}", (int)response.StatusCode, url);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType
                              ?? "image/jpeg";

            // Boyut kontrolü
            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength > options.MaxFileSizeBytes)
            {
                logger.LogWarning(
                    "Resim çok büyük ({Size:N0} bytes): {Url}", contentLength, url);
                return null;
            }

            var data = await response.Content
                .ReadAsByteArrayAsync(cancelToken)
                .ConfigureAwait(false);

            // SHA-256 hash
            var hash = ComputeSha256Hex(data);

            return new DownloadedImage(
                OriginalUrl: url,
                LocalPath:   null,
                Data:        data,
                ContentType: contentType,
                Sha256Hash:  hash,
                SizeBytes:   data.Length
            );
        }, ct).ConfigureAwait(false);
    }

    private static string ComputeSha256Hex(byte[] data)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(data)).ToLowerInvariant();
    }

    private static async Task SaveToDiskAsync(
        DownloadedImage image, string storagePath, CancellationToken ct)
    {
        Directory.CreateDirectory(storagePath);
        var ext = image.ContentType.Contains("png") ? "png"
                : image.ContentType.Contains("gif") ? "gif" : "jpg";
        var fileName = $"{image.Sha256Hash[..16]}.{ext}";
        var fullPath = Path.Combine(storagePath, fileName);

        if (!File.Exists(fullPath) && image.Data is not null)
            await File.WriteAllBytesAsync(fullPath, image.Data.ToArray(), ct).ConfigureAwait(false);
    }
}
