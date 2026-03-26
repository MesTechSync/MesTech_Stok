using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Etsy platform adaptoru — TAM implementasyon (Panel E).
/// Auth: OAuth 2.0 (PKCE) — access token credential'dan alinir.
/// API: https://openapi.etsy.com/v3/
/// Implements IIntegratorAdapter + IOrderCapableAdapter.
///
/// SyncProducts → GET /v3/application/shops/{shopId}/listings (active)
/// GetOrders    → GET /v3/application/shops/{shopId}/receipts
/// UpdateStock  → PUT /v3/application/listings/{listingId}/inventory
/// UpdatePrice  → PUT /v3/application/listings/{listingId} (price field)
/// Categories   → GET /v3/application/seller-taxonomy/nodes
/// </summary>
public sealed class EtsyAdapter : IIntegratorAdapter, IOrderCapableAdapter, IPingableAdapter, IShipmentCapableAdapter, ISettlementCapableAdapter, IClaimCapableAdapter, IInvoiceCapableAdapter, IWebhookCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EtsyAdapter> _logger;
    private readonly EtsyOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);

    // Runtime credential state — set via TestConnectionAsync
    private string _shopId = string.Empty;
    private string _accessToken = string.Empty;
    private bool _isConfigured;

    private const string BaseUrl = "https://openapi.etsy.com/v3";

    public EtsyAdapter(HttpClient httpClient, ILogger<EtsyAdapter> logger,
        IOptions<EtsyOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new EtsyOptions();

        // Seed from options if provided
        if (!string.IsNullOrWhiteSpace(_options.ShopId))
        {
            _shopId = _options.ShopId;
            _accessToken = _options.AccessToken;
            _isConfigured = !string.IsNullOrWhiteSpace(_shopId) &&
                            !string.IsNullOrWhiteSpace(_accessToken);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            // HTTP 429 rate-limit retry — respects Retry-After header or defaults to 2s (Etsy: 10 req/s)
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 5,
                DelayGenerator = args =>
                {
                    if (args.Outcome.Result is { StatusCode: System.Net.HttpStatusCode.TooManyRequests } retryResponse
                        && retryResponse.Headers.RetryAfter is { } retryAfter)
                    {
                        var delay = retryAfter.Delta ?? TimeSpan.FromSeconds(2);
                        return new ValueTask<TimeSpan?>(delay);
                    }
                    return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(2));
                },
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode == 429),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Etsy API rate limited (429). Retry {Attempt} after {Delay}ms",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return default;
                }
            })
            // Server error retry — exponential backoff for 5xx
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                DelayGenerator = args => new ValueTask<TimeSpan?>(
                    TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber))),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Etsy API retry {Attempt} after {Delay}ms",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return default;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
                    .Handle<HttpRequestException>(),
                OnOpened = args =>
                {
                    _logger.LogWarning("{Platform} circuit breaker OPENED for {Duration}s",
                        PlatformCode, args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    // ─────────────────────────────────────────────
    // IIntegratorAdapter — Identity
    // ─────────────────────────────────────────────

    public string PlatformCode => "Etsy";
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => false;

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private void ConfigureCredentials(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        _shopId = credentials.GetValueOrDefault("ShopId", string.Empty);
        _accessToken = credentials.GetValueOrDefault("AccessToken", string.Empty);

        // Also accept ApiKey for x-api-key header (Etsy v3 requires it)
        var apiKey = credentials.GetValueOrDefault("ApiKey", _options.ApiKey);

        _isConfigured = !string.IsNullOrWhiteSpace(_shopId) &&
                        !string.IsNullOrWhiteSpace(_accessToken);

        if (_isConfigured)
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);

            // Etsy v3 requires x-api-key header
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("x-api-key");
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }
        }
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "EtsyAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
    }

    private async Task<HttpResponseMessage> SendWithResilienceAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    // Clone the request for retry (HttpRequestMessage can only be sent once)
                    using var clone = new HttpRequestMessage(request.Method, request.RequestUri);
                    clone.Version = request.Version;

                    if (request.Content != null)
                    {
                        var content = await request.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                        clone.Content = new StringContent(content, Encoding.UTF8, "application/json");
                    }

                    foreach (var header in request.Headers)
                        clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

                    return await _httpClient.SendAsync(clone, token).ConfigureAwait(false);
                },
                ct).ConfigureAwait(false);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    // ─────────────────────────────────────────────
    // IIntegratorAdapter — TestConnection
    // ─────────────────────────────────────────────

    /// <summary>
    /// Tests connection by calling GET /v3/application/shops/{shopId}.
    /// Returns store name and listing count.
    /// </summary>
    public async Task<ConnectionTestResultDto> TestConnectionAsync(
        Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        var sw = Stopwatch.StartNew();
        var result = new ConnectionTestResultDto { PlatformCode = PlatformCode };

        try
        {
            ConfigureCredentials(credentials);

            if (!_isConfigured)
            {
                result.ErrorMessage = "Etsy: ShopId veya AccessToken eksik";
                return result;
            }

            // GET shop info
            var url = $"{BaseUrl}/application/shops/{_shopId}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await SendWithResilienceAsync(request, ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = $"HTTP {response.StatusCode}: {body}";
                return result;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("shop_name", out var nameEl))
                result.StoreName = nameEl.GetString() ?? "Etsy Shop";

            if (doc.RootElement.TryGetProperty("listing_active_count", out var countEl))
                result.ProductCount = countEl.GetInt32();

            result.IsSuccess = true;
            _logger.LogInformation("Etsy TestConnection succeeded: Shop={Shop}, Listings={Count}",
                result.StoreName, result.ProductCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Etsy TestConnectionAsync basarisiz");
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            sw.Stop();
            result.ResponseTime = sw.Elapsed;
        }

        return result;
    }

    // ─────────────────────────────────────────────
    // IIntegratorAdapter — Products
    // ─────────────────────────────────────────────

    /// <summary>
    /// Pulls active listings from Etsy shop with offset pagination.
    /// GET /v3/application/shops/{shopId}/listings/active?limit=100&amp;offset={offset}
    /// </summary>
    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EtsyAdapter.PullProductsAsync called for ShopId={ShopId}", _shopId);

        var products = new List<Product>();

        try
        {
            var offset = 0;
            const int limit = 100; // Etsy max per page
            var hasMore = true;

            while (hasMore)
            {
                var url = $"{BaseUrl}/application/shops/{_shopId}/listings/active?limit={limit}&offset={offset}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await SendWithResilienceAsync(request, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("Etsy PullProducts failed at offset {Offset}: {Status} - {Error}",
                        offset, response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("results", out var results))
                {
                    var count = 0;
                    foreach (var listing in results.EnumerateArray())
                    {
                        var product = MapListingToProduct(listing);
                        products.Add(product);
                        count++;
                    }

                    // Etsy returns count; if less than limit, we've reached the end
                    hasMore = count >= limit;
                    offset += count;
                }
                else
                {
                    hasMore = false;
                }
            }

            _logger.LogInformation("Etsy PullProducts: {Count} products retrieved", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Etsy PullProducts failed");
        }

        return products.AsReadOnly();
    }

    /// <summary>
    /// Creates or updates a listing on Etsy.
    /// POST /v3/application/shops/{shopId}/listings (create).
    /// Requires taxonomy_id, who_made, when_made, is_supply fields (Etsy mandatory).
    /// </summary>
    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EtsyAdapter.PushProductAsync: SKU={SKU}, Name={Name}",
            product.SKU, product.Name);

        try
        {
            var url = $"{BaseUrl}/application/shops/{_shopId}/listings";

            var payload = new Dictionary<string, object>
            {
                ["title"] = product.Name,
                ["description"] = product.Description ?? product.Name,
                ["price"] = product.SalePrice,
                ["quantity"] = Math.Max(product.Stock, 0),
                ["sku"] = new[] { product.SKU },
                // Etsy mandatory fields
                ["who_made"] = "i_did",
                ["when_made"] = "made_to_order",
                ["is_supply"] = false,
                ["taxonomy_id"] = 1, // Default — should be mapped from CategoryId
                ["shipping_profile_id"] = 0 // Required — must be configured
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await SendWithResilienceAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Etsy PushProduct succeeded: SKU={SKU}", product.SKU);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("Etsy PushProduct failed: {Status} - {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Etsy PushProduct failed: SKU={SKU}", product.SKU);
            return false;
        }
    }

    /// <summary>
    /// Updates stock quantity via Etsy Inventory API.
    /// PUT /v3/application/listings/{listingId}/inventory
    /// Note: productId is used as listing_id lookup (via SKU mapping in production).
    /// </summary>
    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EtsyAdapter.PushStockUpdateAsync: ProductId={ProductId}, Qty={Qty}",
            productId, newStock);

        try
        {
            // Step 1: Find listing by searching shop listings for the product
            var listingId = await FindListingIdByProductAsync(productId, ct).ConfigureAwait(false);

            if (string.IsNullOrEmpty(listingId))
            {
                _logger.LogWarning("Etsy PushStockUpdate: listing not found for ProductId={ProductId}", productId);
                return false;
            }

            // Step 2: Get current inventory to preserve offering structure
            var inventoryUrl = $"{BaseUrl}/application/listings/{listingId}/inventory";
            using var getRequest = new HttpRequestMessage(HttpMethod.Get, inventoryUrl);
            var getResponse = await SendWithResilienceAsync(getRequest, ct).ConfigureAwait(false);

            if (!getResponse.IsSuccessStatusCode)
            {
                var error = await getResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Etsy GetInventory failed: {Status} - {Error}", getResponse.StatusCode, error);
                return false;
            }

            var inventoryContent = await getResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var inventoryDoc = JsonDocument.Parse(inventoryContent);

            // Step 3: Update quantity in existing inventory structure
            var updatedInventory = BuildInventoryUpdatePayload(inventoryDoc.RootElement, newStock);

            using var putRequest = new HttpRequestMessage(HttpMethod.Put, inventoryUrl)
            {
                Content = new StringContent(updatedInventory, Encoding.UTF8, "application/json")
            };

            var putResponse = await SendWithResilienceAsync(putRequest, ct).ConfigureAwait(false);

            if (putResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Etsy PushStockUpdate succeeded: ListingId={ListingId}, Qty={Qty}",
                    listingId, newStock);
                return true;
            }

            var putError = await putResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("Etsy PushStockUpdate PUT failed: {Status} - {Error}",
                putResponse.StatusCode, putError);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Etsy PushStockUpdate failed: ProductId={ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Updates listing price via PUT /v3/application/listings/{listingId}.
    /// </summary>
    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EtsyAdapter.PushPriceUpdateAsync: ProductId={ProductId}, Price={Price}",
            productId, newPrice);

        try
        {
            var listingId = await FindListingIdByProductAsync(productId, ct).ConfigureAwait(false);

            if (string.IsNullOrEmpty(listingId))
            {
                _logger.LogWarning("Etsy PushPriceUpdate: listing not found for ProductId={ProductId}", productId);
                return false;
            }

            var url = $"{BaseUrl}/application/listings/{listingId}";
            var payload = new { price = newPrice };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await SendWithResilienceAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Etsy PushPriceUpdate succeeded: ListingId={ListingId}, Price={Price}",
                    listingId, newPrice);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("Etsy PushPriceUpdate failed: {Status} - {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Etsy PushPriceUpdate failed: ProductId={ProductId}", productId);
            return false;
        }
    }

    // ─────────────────────────────────────────────
    // IIntegratorAdapter — Categories
    // ─────────────────────────────────────────────

    /// <summary>
    /// Fetches Etsy seller taxonomy nodes.
    /// GET /v3/application/seller-taxonomy/nodes
    /// </summary>
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("EtsyAdapter.GetCategoriesAsync called");

        var categories = new List<CategoryDto>();

        try
        {
            var url = $"{BaseUrl}/application/seller-taxonomy/nodes";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Note: taxonomy endpoint may work with just API key (no OAuth needed)
            var response = _isConfigured
                ? await SendWithResilienceAsync(request, ct).ConfigureAwait(false)
                : await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Etsy GetCategories failed: {Status} - {Error}", response.StatusCode, error);
                return categories.AsReadOnly();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("results", out var results))
            {
                foreach (var node in results.EnumerateArray())
                {
                    var category = new CategoryDto
                    {
                        PlatformCategoryId = node.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0,
                        Name = node.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty,
                        ParentId = node.TryGetProperty("parent_id", out var parentEl) && parentEl.ValueKind != JsonValueKind.Null
                            ? parentEl.GetInt32()
                            : null
                    };

                    // Parse children recursively
                    if (node.TryGetProperty("children", out var children))
                    {
                        foreach (var child in children.EnumerateArray())
                        {
                            category.SubCategories.Add(new CategoryDto
                            {
                                PlatformCategoryId = child.TryGetProperty("id", out var cIdEl) ? cIdEl.GetInt32() : 0,
                                Name = child.TryGetProperty("name", out var cNameEl) ? cNameEl.GetString() ?? string.Empty : string.Empty,
                                ParentId = category.PlatformCategoryId
                            });
                        }
                    }

                    categories.Add(category);
                }
            }

            _logger.LogInformation("Etsy GetCategories: {Count} top-level categories retrieved", categories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Etsy GetCategories failed");
        }

        return categories.AsReadOnly();
    }

    // ─────────────────────────────────────────────
    // IOrderCapableAdapter — Orders (Receipts)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Pulls orders (receipts) from Etsy.
    /// GET /v3/application/shops/{shopId}/receipts?min_created={since}
    /// </summary>
    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EtsyAdapter.PullOrdersAsync called. Since={Since}", since);

        var orders = new List<ExternalOrderDto>();

        try
        {
            var offset = 0;
            const int limit = 25; // Etsy max for receipts
            var hasMore = true;

            while (hasMore)
            {
                var url = $"{BaseUrl}/application/shops/{_shopId}/receipts?limit={limit}&offset={offset}";

                if (since.HasValue)
                {
                    var unixTimestamp = new DateTimeOffset(since.Value.ToUniversalTime()).ToUnixTimeSeconds();
                    url += $"&min_created={unixTimestamp}";
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await SendWithResilienceAsync(request, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("Etsy PullOrders failed at offset {Offset}: {Status} - {Error}",
                        offset, response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("results", out var results))
                {
                    var count = 0;
                    foreach (var receipt in results.EnumerateArray())
                    {
                        var order = MapReceiptToOrder(receipt);
                        orders.Add(order);
                        count++;
                    }

                    hasMore = count >= limit;
                    offset += count;
                }
                else
                {
                    hasMore = false;
                }
            }

            _logger.LogInformation("Etsy PullOrders: {Count} orders retrieved", orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Etsy PullOrders failed");
        }

        return orders.AsReadOnly();
    }

    /// <summary>
    /// Updates order shipment status — Etsy uses shipment tracking endpoint.
    /// POST /v3/application/shops/{shopId}/receipts/{receiptId}/tracking
    /// </summary>
    public async Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EtsyAdapter.UpdateOrderStatusAsync: ReceiptId={ReceiptId}, Status={Status}",
            packageId, status);

        try
        {
            // Etsy doesn't have a generic status update — only shipment tracking
            // If status is a tracking number, post it as shipment tracking
            var url = $"{BaseUrl}/application/shops/{_shopId}/receipts/{packageId}/tracking";

            var payload = new Dictionary<string, object>
            {
                ["tracking_code"] = status,
                ["carrier_name"] = "other" // Caller should provide proper carrier
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await SendWithResilienceAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Etsy UpdateOrderStatus succeeded: ReceiptId={ReceiptId}", packageId);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("Etsy UpdateOrderStatus failed: {Status} - {Error}",
                response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Etsy UpdateOrderStatus failed: ReceiptId={ReceiptId}", packageId);
            return false;
        }
    }

    // ─────────────────────────────────────────────
    // Private Mapping Helpers
    // ─────────────────────────────────────────────

    private static Product MapListingToProduct(JsonElement listing)
    {
        var product = new Product
        {
            Name = listing.TryGetProperty("title", out var titleEl)
                ? titleEl.GetString() ?? string.Empty
                : string.Empty,
            Description = listing.TryGetProperty("description", out var descEl)
                ? descEl.GetString()
                : null,
            Stock = listing.TryGetProperty("quantity", out var qtyEl) ? qtyEl.GetInt32() : 0,
            IsActive = true
        };

        // Price — Etsy returns price as { amount, divisor, currency_code }
        if (listing.TryGetProperty("price", out var priceObj))
        {
            if (priceObj.TryGetProperty("amount", out var amountEl) &&
                priceObj.TryGetProperty("divisor", out var divisorEl))
            {
                var amount = amountEl.GetInt64();
                var divisor = divisorEl.GetInt32();
                product.SalePrice = divisor > 0 ? (decimal)amount / divisor : 0m;
            }
        }

        // SKU from first sku array element
        if (listing.TryGetProperty("skus", out var skusEl) && skusEl.ValueKind == JsonValueKind.Array)
        {
            var enumerator = skusEl.EnumerateArray();
            if (enumerator.MoveNext())
                product.SKU = enumerator.Current.GetString() ?? string.Empty;
        }

        // Tags as comma-separated
        if (listing.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
        {
            var tagList = new List<string>();
            foreach (var tag in tagsEl.EnumerateArray())
            {
                var tagValue = tag.GetString();
                if (!string.IsNullOrWhiteSpace(tagValue))
                    tagList.Add(tagValue);
            }
            product.Tags = string.Join(",", tagList);
        }

        // Image URL — first image
        if (listing.TryGetProperty("images", out var imagesEl) && imagesEl.ValueKind == JsonValueKind.Array)
        {
            var imageEnumerator = imagesEl.EnumerateArray();
            if (imageEnumerator.MoveNext())
            {
                if (imageEnumerator.Current.TryGetProperty("url_570xN", out var imgUrlEl))
                    product.ImageUrl = imgUrlEl.GetString();
            }
        }

        // Currency
        if (listing.TryGetProperty("price", out var priceForCurrency) &&
            priceForCurrency.TryGetProperty("currency_code", out var currEl))
        {
            product.CurrencyCode = currEl.GetString() ?? "USD";
        }

        return product;
    }

    private static ExternalOrderDto MapReceiptToOrder(JsonElement receipt)
    {
        var order = new ExternalOrderDto
        {
            PlatformCode = "Etsy",
            PlatformOrderId = receipt.TryGetProperty("receipt_id", out var idEl)
                ? idEl.GetInt64().ToString()
                : string.Empty,
            OrderNumber = receipt.TryGetProperty("receipt_id", out var numEl)
                ? $"ETSY-{numEl.GetInt64()}"
                : string.Empty,
            Status = receipt.TryGetProperty("status", out var statusEl)
                ? statusEl.GetString() ?? "unknown"
                : "unknown",
            CustomerName = receipt.TryGetProperty("name", out var nameEl)
                ? nameEl.GetString() ?? string.Empty
                : string.Empty,
            CustomerEmail = receipt.TryGetProperty("buyer_email", out var emailEl)
                ? emailEl.GetString()
                : null,
            Currency = "USD" // Etsy primarily uses USD
        };

        // Total price — grandtotal
        if (receipt.TryGetProperty("grandtotal", out var totalObj))
        {
            if (totalObj.TryGetProperty("amount", out var amountEl) &&
                totalObj.TryGetProperty("divisor", out var divisorEl))
            {
                var amount = amountEl.GetInt64();
                var divisor = divisorEl.GetInt32();
                order.TotalAmount = divisor > 0 ? (decimal)amount / divisor : 0m;
            }

            if (totalObj.TryGetProperty("currency_code", out var currEl))
                order.Currency = currEl.GetString() ?? "USD";
        }

        // Shipping cost
        if (receipt.TryGetProperty("total_shipping_cost", out var shipObj))
        {
            if (shipObj.TryGetProperty("amount", out var shipAmountEl) &&
                shipObj.TryGetProperty("divisor", out var shipDivisorEl))
            {
                var amount = shipAmountEl.GetInt64();
                var divisor = shipDivisorEl.GetInt32();
                order.ShippingCost = divisor > 0 ? (decimal)amount / divisor : 0m;
            }
        }

        // Order date — created_timestamp (Unix seconds)
        if (receipt.TryGetProperty("created_timestamp", out var createdEl))
        {
            var timestamp = createdEl.GetInt64();
            order.OrderDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        // Address
        if (receipt.TryGetProperty("formatted_address", out var addrEl))
            order.CustomerAddress = addrEl.GetString();

        if (receipt.TryGetProperty("city", out var cityEl))
            order.CustomerCity = cityEl.GetString();

        // Transactions (line items)
        if (receipt.TryGetProperty("transactions", out var transactions) &&
            transactions.ValueKind == JsonValueKind.Array)
        {
            foreach (var tx in transactions.EnumerateArray())
            {
                var line = new ExternalOrderLineDto
                {
                    PlatformLineId = tx.TryGetProperty("transaction_id", out var txIdEl)
                        ? txIdEl.GetInt64().ToString()
                        : null,
                    ProductName = tx.TryGetProperty("title", out var txTitleEl)
                        ? txTitleEl.GetString() ?? string.Empty
                        : string.Empty,
                    Quantity = tx.TryGetProperty("quantity", out var txQtyEl)
                        ? txQtyEl.GetInt32()
                        : 1,
                    SKU = tx.TryGetProperty("sku", out var txSkuEl)
                        ? txSkuEl.GetString()
                        : null
                };

                // Line price
                if (tx.TryGetProperty("price", out var txPriceObj))
                {
                    if (txPriceObj.TryGetProperty("amount", out var txAmountEl) &&
                        txPriceObj.TryGetProperty("divisor", out var txDivisorEl))
                    {
                        var amount = txAmountEl.GetInt64();
                        var divisor = txDivisorEl.GetInt32();
                        line.UnitPrice = divisor > 0 ? (decimal)amount / divisor : 0m;
                    }
                }

                line.LineTotal = line.UnitPrice * line.Quantity;
                order.Lines.Add(line);
            }
        }

        return order;
    }

    /// <summary>
    /// Finds an Etsy listing ID by searching shop listings for a matching product.
    /// In production, this would use ProductPlatformMapping table for O(1) lookup.
    /// For now, we search by offset pagination.
    /// </summary>
    private async Task<string?> FindListingIdByProductAsync(Guid productId, CancellationToken ct)
    {
        // In a full implementation, this would look up ProductPlatformMapping table.
        // Fallback: search first 200 listings for a matching SKU (productId.ToString()).
        var targetSku = productId.ToString();
        var offset = 0;
        const int limit = 100;

        for (var page = 0; page < 2; page++)
        {
            var url = $"{BaseUrl}/application/shops/{_shopId}/listings/active?limit={limit}&offset={offset}&includes=images";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await SendWithResilienceAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                break;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("results", out var results)) break;

            foreach (var listing in results.EnumerateArray())
            {
                // Check SKUs
                if (listing.TryGetProperty("skus", out var skusEl) && skusEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var sku in skusEl.EnumerateArray())
                    {
                        if (string.Equals(sku.GetString(), targetSku, StringComparison.OrdinalIgnoreCase))
                        {
                            if (listing.TryGetProperty("listing_id", out var listingIdEl))
                                return listingIdEl.GetInt64().ToString();
                        }
                    }
                }
            }

            offset += limit;
        }

        return null;
    }

    /// <summary>
    /// Builds the inventory update JSON payload, preserving existing offering structure
    /// but updating quantities to the new stock value.
    /// </summary>
    private string BuildInventoryUpdatePayload(JsonElement inventoryRoot, int newQuantity)
    {
        // Etsy inventory update requires the full products array with offerings
        // We update the quantity in all offerings to the new value
        var payload = new Dictionary<string, object>();

        if (inventoryRoot.TryGetProperty("products", out var products) &&
            products.ValueKind == JsonValueKind.Array)
        {
            var productsList = new List<Dictionary<string, object>>();

            foreach (var product in products.EnumerateArray())
            {
                var productDict = new Dictionary<string, object>();

                if (product.TryGetProperty("product_id", out var pidEl))
                    productDict["product_id"] = pidEl.GetInt64();

                if (product.TryGetProperty("sku", out var skuEl))
                    productDict["sku"] = skuEl.GetString() ?? string.Empty;

                // Update offerings with new quantity
                if (product.TryGetProperty("offerings", out var offerings) &&
                    offerings.ValueKind == JsonValueKind.Array)
                {
                    var offeringsList = new List<Dictionary<string, object>>();

                    foreach (var offering in offerings.EnumerateArray())
                    {
                        var offeringDict = new Dictionary<string, object>
                        {
                            ["quantity"] = newQuantity,
                            ["is_enabled"] = true
                        };

                        if (offering.TryGetProperty("offering_id", out var oidEl))
                            offeringDict["offering_id"] = oidEl.GetInt64();

                        if (offering.TryGetProperty("price", out var priceEl))
                        {
                            offeringDict["price"] = new Dictionary<string, object>
                            {
                                ["amount"] = priceEl.TryGetProperty("amount", out var amEl)
                                    ? amEl.GetInt64() : 0,
                                ["divisor"] = priceEl.TryGetProperty("divisor", out var divEl)
                                    ? divEl.GetInt32() : 100,
                                ["currency_code"] = priceEl.TryGetProperty("currency_code", out var curEl)
                                    ? curEl.GetString() ?? "USD" : "USD"
                            };
                        }

                        offeringsList.Add(offeringDict);
                    }

                    productDict["offerings"] = offeringsList;
                }

                // Preserve property values
                if (product.TryGetProperty("property_values", out var propVals))
                    productDict["property_values"] = JsonSerializer.Deserialize<JsonElement>(propVals.GetRawText());

                productsList.Add(productDict);
            }

            payload["products"] = productsList;
        }

        return JsonSerializer.Serialize(payload, _jsonOptions);
    }

    // ── IPingableAdapter ──
    public async Task<bool> PingAsync(CancellationToken ct = default) { try { using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct); cts.CancelAfter(TimeSpan.FromSeconds(5)); var response = await _httpClient.GetAsync($"{BaseUrl}/application/openapi-ping", cts.Token).ConfigureAwait(false); return true; } catch (Exception ex) { _logger.LogWarning(ex, "Etsy ping failed"); return false; } }

    // ── IShipmentCapableAdapter ──
    public async Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber, MesTech.Domain.Enums.CargoProvider provider, CancellationToken ct = default) { _logger.LogInformation("[EtsyAdapter] SendShipment — Receipt:{Receipt}", platformOrderId); try { var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/application/shops/{_shopId}/receipts/{platformOrderId}/tracking") { Content = new StringContent(JsonSerializer.Serialize(new { tracking_code = trackingNumber, carrier_name = provider.ToString() }, _jsonOptions), Encoding.UTF8, "application/json") }; request.Headers.Add("x-api-key", _accessToken); var response = await SendWithResilienceAsync(request, ct).ConfigureAwait(false); return response.IsSuccessStatusCode; } catch (Exception ex) { _logger.LogError(ex, "[EtsyAdapter] SendShipment error"); return false; } }

    // ── ISettlementCapableAdapter ──
    public Task<SettlementDto?> GetSettlementAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default) { _logger.LogWarning("[EtsyAdapter] GetSettlement — Etsy Ledger API requires elevated scope"); return Task.FromResult<SettlementDto?>(null); }
    public Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(DateTime startDate, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CargoInvoiceDto>>(Array.Empty<CargoInvoiceDto>());

    // ── IClaimCapableAdapter ──
    public async Task<IReadOnlyList<ExternalClaimDto>> PullClaimsAsync(DateTime? since = null, CancellationToken ct = default) { _logger.LogInformation("[EtsyAdapter] PullClaims"); try { var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/application/shops/{_shopId}/receipts?was_canceled=true&limit=25"); request.Headers.Add("x-api-key", _accessToken); var response = await SendWithResilienceAsync(request, ct).ConfigureAwait(false); if (!response.IsSuccessStatusCode) return Array.Empty<ExternalClaimDto>(); var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false); using var doc = JsonDocument.Parse(body); var claims = new List<ExternalClaimDto>(); if (doc.RootElement.TryGetProperty("results", out var results)) foreach (var r in results.EnumerateArray()) { var rid = r.TryGetProperty("receipt_id", out var id) ? id.GetInt64().ToString() : ""; claims.Add(new ExternalClaimDto { PlatformClaimId = rid, PlatformCode = "Etsy", Status = "CANCELED" }); } return claims; } catch (Exception ex) { _logger.LogError(ex, "[EtsyAdapter] PullClaims error"); return Array.Empty<ExternalClaimDto>(); } }
    public Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    { _logger.LogDebug("[EtsyAdapter] ApproveClaim not supported — Etsy handles refunds internally"); return Task.FromResult(false); }
    public Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    { _logger.LogDebug("[EtsyAdapter] RejectClaim not supported — Etsy handles refunds internally"); return Task.FromResult(false); }

    // ── IInvoiceCapableAdapter ──
    public Task<bool> SendInvoiceLinkAsync(string shipmentPackageId, string invoiceUrl, CancellationToken ct = default)
    { _logger.LogDebug("[EtsyAdapter] SendInvoiceLink not supported — no invoice API"); return Task.FromResult(false); }
    public Task<bool> SendInvoiceFileAsync(string shipmentPackageId, byte[] pdfBytes, string fileName, CancellationToken ct = default)
    { _logger.LogDebug("[EtsyAdapter] SendInvoiceFile not supported — no invoice API"); return Task.FromResult(false); }

    // ── IWebhookCapableAdapter ──
    public async Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EtsyAdapter.RegisterWebhookAsync: {Url}", callbackUrl);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/application/shops/{_shopId}/webhooks")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { url = callbackUrl, events = new[] { "receipt_created", "receipt_updated" } }, _jsonOptions),
                    Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", _accessToken);
            var response = await SendWithResilienceAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Etsy RegisterWebhook failed: {Status} {Error}", response.StatusCode, error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) { _logger.LogError(ex, "Etsy RegisterWebhook exception"); return false; }
    }

    public async Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EtsyAdapter.UnregisterWebhookAsync");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/application/shops/{_shopId}/webhooks");
            request.Headers.Add("x-api-key", _accessToken);
            var response = await SendWithResilienceAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Etsy UnregisterWebhook failed: {Status} {Error}", response.StatusCode, error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) { _logger.LogError(ex, "Etsy UnregisterWebhook exception"); return false; }
    }

    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(payload);
        var eventType = doc.RootElement.TryGetProperty("type", out var et) ? et.GetString() : "unknown";
        _logger.LogInformation("EtsyAdapter webhook processed: EventType={EventType} PayloadLength={Length}", eventType, payload.Length);
        return Task.CompletedTask;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Configuration Options
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Configuration options for EtsyAdapter.
/// Bind from appsettings.json section "Integrations:Etsy".
/// </summary>
public sealed class EtsyOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:Etsy";

    /// <summary>Etsy Shop ID (numeric).</summary>
    public string ShopId { get; set; } = string.Empty;

    /// <summary>OAuth 2.0 access token (PKCE flow).</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Etsy API key (keystring from developer portal).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>OAuth 2.0 refresh token for token renewal.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Whether the Etsy integration is enabled.</summary>
    public bool Enabled { get; set; }
}
