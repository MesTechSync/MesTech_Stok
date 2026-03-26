using System.Net.Http.Json;
using System.Text.Json;
using MesTech.Application.Interfaces.Dropshipping;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Dropshipping;

/// <summary>
/// IDropshipFeedFetcher implementasyonu — HTTP ile tedarikci API'sinden JSON feed ceker.
/// K1d-05: SyncDropshipProductsHandler icin infrastructure servisi.
/// </summary>
public sealed class HttpDropshipFeedFetcher : IDropshipFeedFetcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpDropshipFeedFetcher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HttpDropshipFeedFetcher(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpDropshipFeedFetcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DropshipFeedItem>> FetchAsync(
        string endpoint, string? apiKey, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            if (!string.IsNullOrWhiteSpace(apiKey))
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");

            _logger.LogInformation("HttpDropshipFeedFetcher — Fetching feed from {Endpoint}", endpoint);

            using var response = await client.GetAsync(endpoint, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var items = await response.Content
                .ReadFromJsonAsync<List<FeedItemDto>>(JsonOptions, ct)
                .ConfigureAwait(false);

            if (items is null || items.Count == 0)
                return [];

            var result = items
                .Where(i => !string.IsNullOrWhiteSpace(i.ExternalId))
                .Select(i => new DropshipFeedItem(i.ExternalId, i.Title, i.Price, i.Stock))
                .ToList();

            _logger.LogInformation(
                "HttpDropshipFeedFetcher — Fetched {Count} products from {Endpoint}",
                result.Count, endpoint);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "HttpDropshipFeedFetcher — Failed to fetch feed from {Endpoint}", endpoint);
            return [];
        }
    }

    /// <summary>
    /// JSON deserialization DTO — esnek alan eslestirme.
    /// </summary>
    private sealed record FeedItemDto
    {
        public string? ExternalId { get; init; }
        public string? Id { get; init; }
        public string? Title { get; init; }
        public string? Name { get; init; }
        public decimal? Price { get; init; }
        public int? Stock { get; init; }
        public int? Quantity { get; init; }
    }
}
