using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Security;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.FeedParsers;

/// <summary>
/// Feed URL health checker. Tests accessibility, content-type, response size,
/// and parse-ability of the first few products.
/// </summary>
public sealed class FeedHealthCheckService
{
    private readonly HttpClient _httpClient;
    private readonly IEnumerable<IFeedParserService> _parsers;
    private readonly ILogger<FeedHealthCheckService> _logger;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public FeedHealthCheckService(
        HttpClient httpClient,
        IEnumerable<IFeedParserService> parsers,
        ILogger<FeedHealthCheckService> logger)
    {
        _httpClient = httpClient;
        _parsers = parsers;
        _logger = logger;
    }

    public async Task<FeedHealthResult> CheckAsync(SupplierFeed feed, CancellationToken ct = default)
    {
        _logger.LogInformation("Feed health check: {FeedName} ({Format}) → {Url}",
            feed.Name, feed.Format, feed.FeedUrl);

        // SSRF guard — reject private/internal network URLs
        if (!SsrfGuard.ValidateUrl(feed.FeedUrl, _logger, nameof(FeedHealthCheckService)))
            return new FeedHealthResult(FeedHealthStatus.Unhealthy,
                "Feed URL rejected by SSRF guard — private network.", null, null, null);

        // Step 1: HTTP HEAD check
        HttpResponseMessage headResponse;
        try
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, feed.FeedUrl);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);
            headResponse = await _httpClient.SendAsync(headRequest, cts.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            return new FeedHealthResult(FeedHealthStatus.Degraded,
                "Feed URL timed out (30s).", null, null, null);
        }
        catch (HttpRequestException ex)
        {
            return new FeedHealthResult(FeedHealthStatus.Unhealthy,
                $"Feed URL unreachable: {ex.Message}", null, null, null);
        }

        if (!headResponse.IsSuccessStatusCode)
        {
            return new FeedHealthResult(FeedHealthStatus.Unhealthy,
                $"Feed URL returned HTTP {(int)headResponse.StatusCode}.",
                headResponse.Content.Headers.ContentType?.MediaType, null, null);
        }

        var contentType = headResponse.Content.Headers.ContentType?.MediaType;
        var contentLength = headResponse.Content.Headers.ContentLength;

        // Step 2: Content-Type validation
        if (!IsAcceptableContentType(feed.Format, contentType))
        {
            _logger.LogWarning("Feed content-type mismatch: expected {Format}, got {ContentType}",
                feed.Format, contentType);
        }

        // Step 3: Response size check
        if (contentLength == 0)
        {
            return new FeedHealthResult(FeedHealthStatus.Unhealthy,
                "Feed URL returned empty response (0 bytes).", contentType, contentLength, null);
        }

        // Step 4: Parse test — download and try parsing first products
        int? parsedCount = null;
        try
        {
            using var getRequest = new HttpRequestMessage(HttpMethod.Get, feed.FeedUrl);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);
            using var getResponse = await _httpClient.SendAsync(getRequest,
                HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);

            if (getResponse.IsSuccessStatusCode)
            {
                await using var stream = await getResponse.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                var parser = _parsers.FirstOrDefault(p => p.SupportedFormat == feed.Format);

                if (parser != null)
                {
                    var defaultMapping = new FeedFieldMapping(null, null, null, null, null, null, null, null);
                    var result = await parser.ParseAsync(stream, defaultMapping, cts.Token).ConfigureAwait(false);
                    parsedCount = result.Products.Count;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feed parse test failed for {FeedName}", feed.Name);
            return new FeedHealthResult(FeedHealthStatus.Degraded,
                $"Feed accessible but parse test failed: {ex.Message}",
                contentType, contentLength, parsedCount);
        }

        if (parsedCount is 0)
        {
            return new FeedHealthResult(FeedHealthStatus.Degraded,
                "Feed accessible but no products could be parsed.",
                contentType, contentLength, 0);
        }

        _logger.LogInformation("Feed health OK: {FeedName} — {Count} products parsed",
            feed.Name, parsedCount);

        return new FeedHealthResult(FeedHealthStatus.Healthy,
            $"Feed healthy — {parsedCount} products parsed successfully.",
            contentType, contentLength, parsedCount);
    }

    private static bool IsAcceptableContentType(FeedFormat format, string? contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return true; // HEAD may not return content-type

        return format switch
        {
            FeedFormat.Xml => contentType.Contains("xml", StringComparison.OrdinalIgnoreCase),
            FeedFormat.Csv => contentType.Contains("csv", StringComparison.OrdinalIgnoreCase) ||
                              contentType.Contains("text/plain", StringComparison.OrdinalIgnoreCase),
            FeedFormat.Json => contentType.Contains("json", StringComparison.OrdinalIgnoreCase),
            FeedFormat.Excel => contentType.Contains("spreadsheet", StringComparison.OrdinalIgnoreCase) ||
                                contentType.Contains("excel", StringComparison.OrdinalIgnoreCase) ||
                                contentType.Contains("octet-stream", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }
}

public enum FeedHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public record FeedHealthResult(
    FeedHealthStatus Status,
    string Message,
    string? ContentType,
    long? ContentLength,
    int? ParsedProductCount);
