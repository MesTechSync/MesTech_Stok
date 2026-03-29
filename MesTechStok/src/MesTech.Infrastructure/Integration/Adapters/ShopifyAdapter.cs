using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Shopify platform adaptoru — TAM implementasyon (Dalga 10 C-01).
/// Auth: X-Shopify-Access-Token header.
/// API version: 2024-01.
/// Implements IIntegratorAdapter + IOrderCapableAdapter + IWebhookCapableAdapter.
/// PullProducts: cursor pagination via Link header.
/// PushStockUpdate: inventory_levels/set.json (requires LocationId).
/// PushPriceUpdate: variants/{id}.json PUT.
/// GetOrders: orders.json?status=open.
/// RegisterWebhook: webhooks.json POST.
/// VerifyWebhookSignature: HMAC-SHA256 (IRON RULE — always verify).
/// </summary>
public sealed class ShopifyAdapter : IIntegratorAdapter, IOrderCapableAdapter, IWebhookCapableAdapter, IShipmentCapableAdapter,
    ISettlementCapableAdapter, IClaimCapableAdapter, IInvoiceCapableAdapter, IPingableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShopifyAdapter> _logger;
    private readonly ShopifyOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    // Shopify leaky bucket: 40 req/s burst, 2 req/s sustained. 4 concurrency prevents 429 storm.
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(4, 4);

    // Runtime credential state — set via TestConnectionAsync
    private string _shopDomain = string.Empty;
    private string _accessToken = string.Empty;
    private string _locationId = string.Empty;
    private string _webhookSecret = string.Empty;
    private bool _isConfigured;

    private const string ApiVersion = "2024-01";

    public ShopifyAdapter(HttpClient httpClient, ILogger<ShopifyAdapter> logger,
        IOptions<ShopifyOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new ShopifyOptions();
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);

        // Seed from options if provided
        if (!string.IsNullOrWhiteSpace(_options.ShopDomain))
        {
            _shopDomain = _options.ShopDomain.TrimEnd('/');
            _accessToken = _options.AccessToken;
            _locationId = _options.LocationId;
            _webhookSecret = _options.WebhookSecret;
            _isConfigured = !string.IsNullOrWhiteSpace(_shopDomain) &&
                            !string.IsNullOrWhiteSpace(_accessToken);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            // HTTP 429 rate-limit retry — Shopify leaky bucket (40 req/s, Retry-After header)
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
                    .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "[ShopifyAdapter] Rate limited (429). Retry {Attempt} after {Delay}ms",
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
                        "[ShopifyAdapter] API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[ShopifyAdapter] Circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    private async Task<HttpResponseMessage> ThrottledExecuteAsync(
        Func<CancellationToken, ValueTask<HttpResponseMessage>> action,
        CancellationToken ct)
    {
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _retryPipeline.ExecuteAsync(action, ct).ConfigureAwait(false);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    // ─────────────────────────────────────────────
    // IIntegratorAdapter — Identity
    // ─────────────────────────────────────────────

    public string PlatformCode => "Shopify";
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private string BaseUrl => $"https://{_shopDomain}/admin/api/{ApiVersion}";

    private void ConfigureCredentials(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        _shopDomain = credentials.GetValueOrDefault("ShopDomain", string.Empty).TrimEnd('/');
        _accessToken = credentials.GetValueOrDefault("AccessToken", string.Empty);
        _locationId = credentials.GetValueOrDefault("LocationId", string.Empty);
        _webhookSecret = credentials.GetValueOrDefault("WebhookSecret", string.Empty);

        _isConfigured = !string.IsNullOrWhiteSpace(_shopDomain) &&
                        !string.IsNullOrWhiteSpace(_accessToken);

        if (_isConfigured)
        {
            // Credentials stored in fields — applied per-request via CreateAuthenticatedRequest
        }
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Shopify-Access-Token", _accessToken);
        return request;
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, HttpContent content)
    {
        var request = CreateAuthenticatedRequest(method, url);
        request.Content = content;
        return request;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "ShopifyAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
    }

    // ─────────────────────────────────────────────
    // IIntegratorAdapter — TestConnection
    // ─────────────────────────────────────────────

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
                result.ErrorMessage = "Shopify: ShopDomain veya AccessToken eksik";
                return result;
            }

            // Verify by hitting shop.json — returns store info
            var url = $"{BaseUrl}/shop.json";
            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = $"HTTP {response.StatusCode}: {body}";
                return result;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("shop", out var shop))
            {
                result.StoreName = shop.TryGetProperty("name", out var nameEl)
                    ? nameEl.GetString() ?? "Shopify Store"
                    : "Shopify Store";
            }

            // Count products
            var countUrl = $"{BaseUrl}/products/count.json";
            var countResponse = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, countUrl);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);
            if (countResponse.IsSuccessStatusCode)
            {
                var countContent = await countResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var countDoc = JsonDocument.Parse(countContent);
                if (countDoc.RootElement.TryGetProperty("count", out var countEl))
                    result.ProductCount = countEl.GetInt32();
            }

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify TestConnectionAsync basarisiz");
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
    /// Pulls all products from Shopify using cursor pagination (Link header: rel="next").
    /// GET /admin/api/2024-01/products.json?limit=250&amp;page_info={cursor}
    /// </summary>
    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.PullProductsAsync called");

        var products = new List<Product>();

        try
        {
            // Initial request — 250 is Shopify's max page size
            var url = $"{BaseUrl}/products.json?limit=250";

            while (!string.IsNullOrEmpty(url))
            {
                var response = await ThrottledExecuteAsync(
                    async token =>
                    {
                        using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
                        return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("Shopify PullProducts failed: {Status} - {Error}", response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("products", out var productsArr))
                {
                    foreach (var productEl in productsArr.EnumerateArray())
                    {
                        var shopifyProduct = ParseShopifyProduct(productEl);

                        // Map each variant to a Product — first variant becomes primary
                        if (shopifyProduct.Variants is { Count: > 0 })
                        {
                            var firstVariant = shopifyProduct.Variants[0];
                            products.Add(new Product
                            {
                                Name = shopifyProduct.Title ?? string.Empty,
                                SKU = firstVariant.Sku ?? string.Empty,
                                Stock = firstVariant.InventoryQuantity,
                                SalePrice = decimal.TryParse(firstVariant.Price,
                                    NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                                    ? price
                                    : 0m
                            });
                        }
                    }
                }

                // Cursor pagination — extract next URL from Link header
                url = ExtractNextLinkUrl(response);
            }

            _logger.LogInformation("Shopify PullProducts: {Count} products retrieved", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify PullProducts failed");
        }

        return products.AsReadOnly();
    }

    /// <summary>
    /// Extracts the next page URL from the Shopify Link header.
    /// Link: &lt;https://store.myshopify.com/admin/api/2024-01/products.json?page_info=xyz&gt;; rel="next"
    /// </summary>
    private static string? ExtractNextLinkUrl(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var linkValues))
            return null;

        foreach (var link in linkValues)
        {
            // Parse comma-separated link entries: <url>; rel="next", <url>; rel="previous"
            var parts = link.Split(',');
            foreach (var part in parts)
            {
                var segments = part.Trim().Split(';');
                if (segments.Length < 2) continue;

                var rel = segments[1].Trim();
                if (!rel.Contains("\"next\"", StringComparison.OrdinalIgnoreCase)) continue;

                // Extract URL from angle brackets
                var rawUrl = segments[0].Trim();
                if (rawUrl.StartsWith('<') && rawUrl.EndsWith('>'))
                    return rawUrl[1..^1];
            }
        }

        return null;
    }

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        // Shopify product creation requires metafields, images, variants — multi-step.
        // Use PushStockUpdateAsync / PushPriceUpdateAsync for existing product updates.
        _logger.LogWarning(
            "ShopifyAdapter.PushProductAsync — full product creation requires variant + inventory flow. " +
            "SKU={SKU}. Use PushStockUpdateAsync/PushPriceUpdateAsync for existing products.",
            product.SKU);
        return Task.FromResult(false);
    }

    /// <summary>
    /// Updates inventory level for a product variant identified by SKU.
    /// Flow: GET /variants.json?sku={sku} → find inventory_item_id → POST inventory_levels/set.json
    /// Requires LocationId to be configured.
    /// </summary>
    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.PushStockUpdateAsync: ProductId={ProductId} qty={Qty}",
            productId, newStock);

        if (string.IsNullOrWhiteSpace(_locationId))
        {
            _logger.LogWarning("ShopifyAdapter.PushStockUpdateAsync — LocationId yapilandirilmamis. " +
                               "Stok guncellemesi icin LocationId gereklidir.");
            return false;
        }

        try
        {
            // Step 1: Find variant by productId (used as SKU in Shopify context)
            var sku = productId.ToString();
            var variantsUrl = $"{BaseUrl}/variants.json?fields=id,sku,inventory_item_id&limit=250";
            var variantResponse = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, variantsUrl);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!variantResponse.IsSuccessStatusCode)
            {
                var error = await variantResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Shopify StockUpdate GetVariants failed: {Status} - {Error}",
                    variantResponse.StatusCode, error);
                return false;
            }

            var variantContent = await variantResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var variantDoc = JsonDocument.Parse(variantContent);

            if (!variantDoc.RootElement.TryGetProperty("variants", out var variantsArr))
            {
                _logger.LogWarning("Shopify StockUpdate: no variants found for SKU={SKU}", sku);
                return false;
            }

            long inventoryItemId = 0;
            foreach (var variantEl in variantsArr.EnumerateArray())
            {
                var variantSku = variantEl.TryGetProperty("sku", out var skuEl)
                    ? skuEl.GetString() ?? ""
                    : "";

                if (!string.Equals(variantSku, sku, StringComparison.OrdinalIgnoreCase)) continue;

                if (variantEl.TryGetProperty("inventory_item_id", out var invItemEl))
                    inventoryItemId = invItemEl.GetInt64();
                break;
            }

            if (inventoryItemId == 0)
            {
                _logger.LogWarning("Shopify StockUpdate: variant not found for SKU={SKU}", sku);
                return false;
            }

            // Step 2: POST inventory_levels/set.json
            var payload = new
            {
                location_id = long.Parse(_locationId, CultureInfo.InvariantCulture),
                inventory_item_id = inventoryItemId,
                available = newStock
            };

            var json = JsonSerializer.Serialize(payload);
            using var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var setUrl = $"{BaseUrl}/inventory_levels/set.json";
            var setResponse = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Post, setUrl, requestContent);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!setResponse.IsSuccessStatusCode)
            {
                var error = await setResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Shopify StockUpdate set failed: {Status} - {Error}",
                    setResponse.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Shopify StockUpdate success: SKU={SKU} qty={Qty}", sku, newStock);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify StockUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Updates price of a variant identified by SKU.
    /// Flow: GET /variants.json?sku={sku} → find variantId → PUT /variants/{id}.json
    /// </summary>
    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.PushPriceUpdateAsync: ProductId={ProductId} price={Price}",
            productId, newPrice);

        try
        {
            var sku = productId.ToString();

            // Step 1: Find variant id by SKU
            var variantsUrl = $"{BaseUrl}/variants.json?fields=id,sku&limit=250";
            var variantResponse = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, variantsUrl);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!variantResponse.IsSuccessStatusCode)
            {
                var error = await variantResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Shopify PriceUpdate GetVariants failed: {Status} - {Error}",
                    variantResponse.StatusCode, error);
                return false;
            }

            var variantContent = await variantResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var variantDoc = JsonDocument.Parse(variantContent);

            if (!variantDoc.RootElement.TryGetProperty("variants", out var variantsArr))
            {
                _logger.LogWarning("Shopify PriceUpdate: no variants found for SKU={SKU}", sku);
                return false;
            }

            long variantId = 0;
            foreach (var variantEl in variantsArr.EnumerateArray())
            {
                var variantSku = variantEl.TryGetProperty("sku", out var skuEl)
                    ? skuEl.GetString() ?? ""
                    : "";

                if (!string.Equals(variantSku, sku, StringComparison.OrdinalIgnoreCase)) continue;

                if (variantEl.TryGetProperty("id", out var idEl))
                    variantId = idEl.GetInt64();
                break;
            }

            if (variantId == 0)
            {
                _logger.LogWarning("Shopify PriceUpdate: variant not found for SKU={SKU}", sku);
                return false;
            }

            // Step 2: PUT /variants/{id}.json
            var payload = new
            {
                variant = new
                {
                    id = variantId,
                    price = newPrice.ToString("F2", CultureInfo.InvariantCulture)
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var putUrl = $"{BaseUrl}/variants/{variantId}.json";
            var putResponse = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Put, putUrl, requestContent);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!putResponse.IsSuccessStatusCode)
            {
                var error = await putResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Shopify PriceUpdate PUT failed: {Status} - {Error}",
                    putResponse.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Shopify PriceUpdate success: SKU={SKU} price={Price}", sku, newPrice);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify PriceUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Returns Shopify's product categories (custom collections as top-level categories).
    /// GET /admin/api/2024-01/custom_collections.json
    /// </summary>
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.GetCategoriesAsync called");

        try
        {
            var url = $"{BaseUrl}/custom_collections.json?fields=id,title&limit=250";
            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Shopify GetCategories failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<CategoryDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var categories = new List<CategoryDto>();

            if (doc.RootElement.TryGetProperty("custom_collections", out var collectionsArr))
            {
                foreach (var collectionEl in collectionsArr.EnumerateArray())
                {
                    var id = collectionEl.TryGetProperty("id", out var idEl) ? (int)idEl.GetInt64() : 0;
                    var title = collectionEl.TryGetProperty("title", out var titleEl)
                        ? titleEl.GetString() ?? string.Empty
                        : string.Empty;

                    categories.Add(new CategoryDto
                    {
                        PlatformCategoryId = id,
                        Name = title,
                        ParentId = null
                    });
                }
            }

            _logger.LogInformation("Shopify GetCategories: {Count} collections retrieved", categories.Count);
            return categories.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify GetCategories failed");
            return Array.Empty<CategoryDto>();
        }
    }

    // ─────────────────────────────────────────────
    // IOrderCapableAdapter — Orders
    // ─────────────────────────────────────────────

    /// <summary>
    /// Pulls open orders from Shopify.
    /// GET /admin/api/2024-01/orders.json?status=open&amp;limit=250&amp;created_at_min={since}
    /// Uses Link header cursor pagination.
    /// </summary>
    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.PullOrdersAsync since={Since}", since);

        var orders = new List<ExternalOrderDto>();

        try
        {
            var sinceStr = since.HasValue
                ? since.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                : DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

            var url = $"{BaseUrl}/orders.json?status=open&limit=250&created_at_min={Uri.EscapeDataString(sinceStr)}";

            while (!string.IsNullOrEmpty(url))
            {
                var response = await ThrottledExecuteAsync(
                    async token =>
                    {
                        using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
                        return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("Shopify PullOrders failed: {Status} - {Error}", response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("orders", out var ordersArr))
                    break;

                foreach (var orderEl in ordersArr.EnumerateArray())
                {
                    orders.Add(MapShopifyOrder(orderEl));
                }

                // Cursor pagination
                url = ExtractNextLinkUrl(response);
            }

            _logger.LogInformation("Shopify PullOrders: {Count} orders retrieved", orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify PullOrders failed");
        }

        return orders.AsReadOnly();
    }

    private ExternalOrderDto MapShopifyOrder(JsonElement orderEl)
    {
        var orderId = orderEl.TryGetProperty("id", out var oidEl)
            ? oidEl.GetInt64().ToString(CultureInfo.InvariantCulture)
            : string.Empty;

        var orderNumber = orderEl.TryGetProperty("name", out var nameEl)
            ? nameEl.GetString() ?? orderId
            : orderId;

        var status = orderEl.TryGetProperty("financial_status", out var statusEl)
            ? statusEl.GetString() ?? "open"
            : "open";

        var orderDate = DateTime.UtcNow;
        if (orderEl.TryGetProperty("created_at", out var createdEl) &&
            DateTime.TryParse(createdEl.GetString(), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var parsedDate))
        {
            orderDate = parsedDate;
        }

        DateTime? lastModified = null;
        if (orderEl.TryGetProperty("updated_at", out var updatedEl) &&
            DateTime.TryParse(updatedEl.GetString(), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var parsedMod))
        {
            lastModified = parsedMod;
        }

        var currency = orderEl.TryGetProperty("currency", out var currEl)
            ? currEl.GetString() ?? "TRY"
            : "TRY";

        var totalAmount = 0m;
        if (orderEl.TryGetProperty("total_price", out var totalEl) &&
            decimal.TryParse(totalEl.GetString(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var parsedTotal))
        {
            totalAmount = parsedTotal;
        }

        var shippingCost = default(decimal?);
        if (orderEl.TryGetProperty("total_shipping_price_set", out var shippingSet) &&
            shippingSet.TryGetProperty("shop_money", out var shopMoney) &&
            shopMoney.TryGetProperty("amount", out var shippingAmountEl) &&
            decimal.TryParse(shippingAmountEl.GetString(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var parsedShipping))
        {
            shippingCost = parsedShipping;
        }

        var discountAmount = default(decimal?);
        if (orderEl.TryGetProperty("total_discounts", out var discEl) &&
            decimal.TryParse(discEl.GetString(), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var parsedDisc) && parsedDisc > 0)
        {
            discountAmount = parsedDisc;
        }

        var order = new ExternalOrderDto
        {
            PlatformCode = PlatformCode,
            PlatformOrderId = orderId,
            OrderNumber = orderNumber,
            Status = status,
            OrderDate = orderDate,
            LastModifiedDate = lastModified,
            TotalAmount = totalAmount,
            DiscountAmount = discountAmount,
            ShippingCost = shippingCost,
            Currency = currency
        };

        // Customer info
        if (orderEl.TryGetProperty("customer", out var customerEl))
        {
            var firstName = customerEl.TryGetProperty("first_name", out var fnEl)
                ? fnEl.GetString() ?? ""
                : "";
            var lastName = customerEl.TryGetProperty("last_name", out var lnEl)
                ? lnEl.GetString() ?? ""
                : "";
            order.CustomerName = $"{firstName} {lastName}".Trim();
            order.CustomerEmail = customerEl.TryGetProperty("email", out var emailEl)
                ? emailEl.GetString()
                : null;
            order.CustomerPhone = customerEl.TryGetProperty("phone", out var phoneEl)
                ? phoneEl.GetString()
                : null;
        }

        // Shipping address
        if (orderEl.TryGetProperty("shipping_address", out var addrEl))
        {
            var address1 = addrEl.TryGetProperty("address1", out var a1El) ? a1El.GetString() ?? "" : "";
            var address2 = addrEl.TryGetProperty("address2", out var a2El) ? a2El.GetString() ?? "" : "";
            order.CustomerAddress = string.IsNullOrEmpty(address2)
                ? address1
                : $"{address1} {address2}".Trim();

            order.CustomerCity = addrEl.TryGetProperty("city", out var cityEl)
                ? cityEl.GetString()
                : null;

            if (order.CustomerPhone is null && addrEl.TryGetProperty("phone", out var addrPhoneEl))
                order.CustomerPhone = addrPhoneEl.GetString();

            if (string.IsNullOrEmpty(order.CustomerName))
            {
                order.CustomerName = addrEl.TryGetProperty("name", out var addrNameEl)
                    ? addrNameEl.GetString() ?? ""
                    : "";
            }
        }

        // Order lines (line_items)
        if (orderEl.TryGetProperty("line_items", out var lineItems))
        {
            foreach (var lineEl in lineItems.EnumerateArray())
            {
                var lineId = lineEl.TryGetProperty("id", out var liEl)
                    ? liEl.GetInt64().ToString(CultureInfo.InvariantCulture)
                    : null;

                var sku = lineEl.TryGetProperty("sku", out var skuEl)
                    ? skuEl.GetString()
                    : null;

                var productTitle = lineEl.TryGetProperty("title", out var titleEl)
                    ? titleEl.GetString() ?? ""
                    : "";

                var qty = lineEl.TryGetProperty("quantity", out var qtyEl)
                    ? qtyEl.GetInt32()
                    : 1;

                var unitPrice = 0m;
                if (lineEl.TryGetProperty("price", out var priceEl) &&
                    decimal.TryParse(priceEl.GetString(), NumberStyles.Number,
                        CultureInfo.InvariantCulture, out var parsedPrice))
                {
                    unitPrice = parsedPrice;
                }

                var lineDiscount = default(decimal?);
                if (lineEl.TryGetProperty("total_discount", out var ldEl) &&
                    decimal.TryParse(ldEl.GetString(), NumberStyles.Number,
                        CultureInfo.InvariantCulture, out var parsedLd) && parsedLd > 0)
                {
                    lineDiscount = parsedLd;
                }

                // Shopify tax lines: array of { rate, price, title }
                var taxRate = 0m;
                if (lineEl.TryGetProperty("tax_lines", out var taxLines) &&
                    taxLines.GetArrayLength() > 0)
                {
                    foreach (var taxLine in taxLines.EnumerateArray())
                    {
                        if (taxLine.TryGetProperty("rate", out var rateEl))
                            taxRate += rateEl.GetDecimal() * 100m; // rate is 0-1, we store as 0-100
                    }
                }

                order.Lines.Add(new ExternalOrderLineDto
                {
                    PlatformLineId = lineId,
                    SKU = sku,
                    ProductName = productTitle,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    DiscountAmount = lineDiscount,
                    TaxRate = taxRate,
                    LineTotal = unitPrice * qty
                });
            }
        }

        return order;
    }

    /// <summary>
    /// Updates order fulfillment status. Shopify uses fulfillments endpoint.
    /// For simplicity this marks the order as fulfilled via POST /orders/{id}/fulfillments.json.
    /// Status "cancelled" triggers POST /orders/{id}/cancel.json.
    /// </summary>
    public async Task<bool> UpdateOrderStatusAsync(string packageId, string status,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.UpdateOrderStatusAsync: OrderId={OrderId} Status={Status}",
            packageId, status);

        if (string.IsNullOrWhiteSpace(packageId))
        {
            _logger.LogWarning("ShopifyAdapter.UpdateOrderStatusAsync — packageId/orderId gereklidir");
            return false;
        }

        try
        {
            string url;
            string payload;

            if (string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                url = $"{BaseUrl}/orders/{packageId}/cancel.json";
                payload = "{}";
            }
            else
            {
                // Mark as fulfilled
                url = $"{BaseUrl}/orders/{packageId}/fulfillments.json";
                payload = JsonSerializer.Serialize(new { fulfillment = new { notify_customer = true } });
            }

            using var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Post, url, requestContent);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Shopify UpdateOrderStatus failed: {Status} - {Error}",
                    response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Shopify UpdateOrderStatus success: OrderId={OrderId} Status={Status}",
                packageId, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify UpdateOrderStatus exception: {OrderId}", packageId);
            return false;
        }
    }

    // ─────────────────────────────────────────────
    // IWebhookCapableAdapter — Webhooks
    // ─────────────────────────────────────────────

    /// <summary>
    /// Registers a webhook for orders/create and orders/updated topics.
    /// POST /admin/api/2024-01/webhooks.json
    /// </summary>
    public async Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.RegisterWebhookAsync: CallbackUrl={Url}", callbackUrl);

        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            _logger.LogWarning("ShopifyAdapter.RegisterWebhookAsync — callbackUrl gereklidir");
            return false;
        }

        try
        {
            var topics = new[] { "orders/create", "orders/updated", "orders/cancelled" };
            var allSuccess = true;

            foreach (var topic in topics)
            {
                var payload = JsonSerializer.Serialize(new
                {
                    webhook = new
                    {
                        topic,
                        address = callbackUrl,
                        format = "json"
                    }
                });

                using var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");
                var url = $"{BaseUrl}/webhooks.json";
                var response = await ThrottledExecuteAsync(
                    async token =>
                    {
                        using var request = CreateAuthenticatedRequest(HttpMethod.Post, url, requestContent);
                        return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("Shopify RegisterWebhook failed for topic={Topic}: {Status} - {Error}",
                        topic, response.StatusCode, error);
                    allSuccess = false;
                }
                else
                {
                    _logger.LogInformation("Shopify webhook registered: topic={Topic} url={Url}", topic, callbackUrl);
                }
            }

            return allSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify RegisterWebhook exception");
            return false;
        }
    }

    /// <summary>
    /// Lists and deletes all registered webhooks for this store.
    /// GET /webhooks.json → DELETE /webhooks/{id}.json per entry.
    /// </summary>
    public async Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.UnregisterWebhookAsync called");

        try
        {
            var listUrl = $"{BaseUrl}/webhooks.json";
            var listResponse = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, listUrl);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!listResponse.IsSuccessStatusCode)
            {
                var error = await listResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Shopify UnregisterWebhook list failed: {Status} - {Error}",
                    listResponse.StatusCode, error);
                return false;
            }

            var listContent = await listResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var listDoc = JsonDocument.Parse(listContent);

            if (!listDoc.RootElement.TryGetProperty("webhooks", out var webhooksArr))
                return true; // None registered

            var allDeleted = true;
            foreach (var webhookEl in webhooksArr.EnumerateArray())
            {
                if (!webhookEl.TryGetProperty("id", out var idEl)) continue;

                var webhookId = idEl.GetInt64();
                var deleteUrl = $"{BaseUrl}/webhooks/{webhookId}.json";
                using var deleteRequest = CreateAuthenticatedRequest(HttpMethod.Delete, deleteUrl);
                var deleteResponse = await _httpClient.SendAsync(deleteRequest, ct).ConfigureAwait(false);

                if (!deleteResponse.IsSuccessStatusCode)
                {
                    var errorBody = await deleteResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("Shopify UnregisterWebhook delete failed for id={Id}: {Status} {Error}",
                        webhookId, deleteResponse.StatusCode, errorBody);
                    allDeleted = false;
                }
            }

            _logger.LogInformation("Shopify UnregisterWebhook complete");
            return allDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify UnregisterWebhook exception");
            return false;
        }
    }

    /// <summary>
    /// Processes incoming Shopify webhook payload.
    /// Verifies HMAC-SHA256 signature when WebhookSecret is configured (IRON RULE).
    /// Payload format: JSON string prefixed with "X-Shopify-Hmac-Sha256:{base64Hmac}|{payload}"
    /// Convention used here: raw JSON payload is passed directly; signature checked separately.
    /// </summary>
    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            _logger.LogWarning("ShopifyAdapter.ProcessWebhookPayloadAsync — bos payload");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Shopify webhook payload received ({Length} bytes)", payload.Length);

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var orderId = root.TryGetProperty("id", out var oidEl)
                ? oidEl.GetInt64().ToString(CultureInfo.InvariantCulture)
                : "unknown";

            var financialStatus = root.TryGetProperty("financial_status", out var fsEl)
                ? fsEl.GetString() ?? ""
                : "";

            _logger.LogInformation(
                "Shopify webhook processed: OrderId={OrderId} FinancialStatus={Status}",
                orderId, financialStatus);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Shopify webhook payload JSON parse hatasi");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies Shopify webhook HMAC-SHA256 signature.
    /// Shopify sends X-Shopify-Hmac-Sha256 header = Base64(HMAC-SHA256(secret, rawBody)).
    /// IRON RULE: Always verify before trusting webhook data.
    /// </summary>
    /// <param name="rawBody">Raw request body bytes.</param>
    /// <param name="signatureHeader">Value of X-Shopify-Hmac-Sha256 header.</param>
    /// <returns>True if signature is valid.</returns>
    public bool VerifyWebhookSignature(byte[] rawBody, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_webhookSecret))
        {
            _logger.LogWarning("Shopify webhook dogrulama atlandi — WebhookSecret yapilandirilmamis");
            return false;
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            _logger.LogWarning("Shopify webhook dogrulama hatasi — X-Shopify-Hmac-Sha256 header eksik");
            return false;
        }

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(_webhookSecret);
            using var hmac = new HMACSHA256(keyBytes);
            var computedHash = hmac.ComputeHash(rawBody);
            var computedBase64 = Convert.ToBase64String(computedHash);

            // Constant-time comparison to prevent timing attacks
            var isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedBase64),
                Encoding.UTF8.GetBytes(signatureHeader));

            if (!isValid)
                _logger.LogWarning("Shopify webhook HMAC dogrulama BASARISIZ — gecersiz imza");

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify VerifyWebhookSignature exception");
            return false;
        }
    }

    // ─────────────────────────────────────────────
    // IShipmentCapableAdapter — Shipment
    // ─────────────────────────────────────────────

    /// <summary>
    /// Sends shipment notification to Shopify by creating a fulfillment on the order.
    /// POST /admin/api/2024-01/orders/{orderId}/fulfillments.json
    /// Also implements IShipmentCapableAdapter.SendShipmentAsync.
    /// </summary>
    public async Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber,
        CargoProvider provider, CancellationToken ct = default)
    {
        var shipment = new ShipmentInfoDto
        {
            TrackingNumber = trackingNumber,
            TrackingCompany = provider.ToString(),
            NotifyCustomer = true
        };
        return await SendShipmentAsync(platformOrderId, shipment, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a fulfillment with tracking info on a Shopify order.
    /// POST /admin/api/2024-01/orders/{orderId}/fulfillments.json
    /// Body: { "fulfillment": { "tracking_number": "...", "tracking_company": "...", "tracking_url": "...", "notify_customer": true } }
    /// </summary>
    public async Task<bool> SendShipmentAsync(string platformOrderId, ShipmentInfoDto shipment,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(shipment);
        _logger.LogInformation(
            "ShopifyAdapter.SendShipmentAsync: OrderId={OrderId} Tracking={Tracking}",
            platformOrderId, shipment.TrackingNumber);

        if (string.IsNullOrWhiteSpace(platformOrderId))
        {
            _logger.LogWarning("ShopifyAdapter.SendShipmentAsync — platformOrderId gereklidir");
            return false;
        }

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                fulfillment = new
                {
                    tracking_number = shipment.TrackingNumber,
                    tracking_company = shipment.TrackingCompany,
                    tracking_url = shipment.TrackingUrl,
                    notify_customer = shipment.NotifyCustomer
                }
            });

            using var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");
            var url = $"{BaseUrl}/orders/{Uri.EscapeDataString(platformOrderId)}/fulfillments.json";
            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Post, url, requestContent);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Shopify SendShipment failed: {Status} - {Error}",
                    response.StatusCode, error);
                return false;
            }

            _logger.LogInformation(
                "Shopify SendShipment success: OrderId={OrderId} Tracking={Tracking}",
                platformOrderId, shipment.TrackingNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify SendShipment exception: OrderId={OrderId}", platformOrderId);
            return false;
        }
    }

    // ─────────────────────────────────────────────
    // Extended methods — Inventory Locations & Variants
    // ─────────────────────────────────────────────

    /// <summary>
    /// Lists all inventory locations for the Shopify store.
    /// GET /admin/api/2024-01/locations.json
    /// </summary>
    public async Task<IReadOnlyList<InventoryLocationDto>> GetInventoryLocationsAsync(
        CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.GetInventoryLocationsAsync called");

        try
        {
            var url = $"{BaseUrl}/locations.json";
            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Shopify GetInventoryLocations failed: {Status} - {Error}",
                    response.StatusCode, error);
                return Array.Empty<InventoryLocationDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var locations = new List<InventoryLocationDto>();

            if (doc.RootElement.TryGetProperty("locations", out var locationsArr))
            {
                foreach (var locEl in locationsArr.EnumerateArray())
                {
                    var id = locEl.TryGetProperty("id", out var idEl)
                        ? idEl.GetInt64().ToString(CultureInfo.InvariantCulture)
                        : string.Empty;

                    var name = locEl.TryGetProperty("name", out var nameEl)
                        ? nameEl.GetString() ?? string.Empty
                        : string.Empty;

                    var address1 = locEl.TryGetProperty("address1", out var a1El)
                        ? a1El.GetString() ?? string.Empty
                        : string.Empty;

                    var address2 = locEl.TryGetProperty("address2", out var a2El)
                        ? a2El.GetString() ?? string.Empty
                        : string.Empty;

                    var address = string.IsNullOrEmpty(address2)
                        ? address1
                        : $"{address1} {address2}".Trim();

                    var active = locEl.TryGetProperty("active", out var activeEl) && activeEl.GetBoolean();

                    locations.Add(new InventoryLocationDto
                    {
                        LocationId = id,
                        Name = name,
                        Address = string.IsNullOrEmpty(address) ? null : address,
                        Active = active
                    });
                }
            }

            _logger.LogInformation("Shopify GetInventoryLocations: {Count} locations retrieved",
                locations.Count);
            return locations.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify GetInventoryLocations failed");
            return Array.Empty<InventoryLocationDto>();
        }
    }

    /// <summary>
    /// Lists variants for a specific Shopify product.
    /// GET /admin/api/2024-01/products/{productId}/variants.json
    /// </summary>
    public async Task<IReadOnlyList<ProductVariantDto>> GetProductVariantsAsync(
        string productId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.GetProductVariantsAsync: ProductId={ProductId}", productId);

        if (string.IsNullOrWhiteSpace(productId))
        {
            _logger.LogWarning("ShopifyAdapter.GetProductVariantsAsync — productId gereklidir");
            return Array.Empty<ProductVariantDto>();
        }

        try
        {
            var url = $"{BaseUrl}/products/{Uri.EscapeDataString(productId)}/variants.json";
            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Shopify GetProductVariants failed: {Status} - {Error}",
                    response.StatusCode, error);
                return Array.Empty<ProductVariantDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var variants = new List<ProductVariantDto>();

            if (doc.RootElement.TryGetProperty("variants", out var variantsArr))
            {
                foreach (var vEl in variantsArr.EnumerateArray())
                {
                    variants.Add(new ProductVariantDto
                    {
                        VariantId = vEl.TryGetProperty("id", out var idEl)
                            ? idEl.GetInt64().ToString(CultureInfo.InvariantCulture)
                            : string.Empty,
                        Sku = vEl.TryGetProperty("sku", out var skuEl)
                            ? skuEl.GetString()
                            : null,
                        Title = vEl.TryGetProperty("title", out var titleEl)
                            ? titleEl.GetString()
                            : null,
                        Price = vEl.TryGetProperty("price", out var priceEl)
                            ? priceEl.GetString()
                            : null,
                        StockQuantity = vEl.TryGetProperty("inventory_quantity", out var iqEl)
                            ? iqEl.GetInt32()
                            : 0,
                        ManageStock = vEl.TryGetProperty("inventory_management", out var imEl)
                            && !string.IsNullOrEmpty(imEl.GetString())
                    });
                }
            }

            _logger.LogInformation("Shopify GetProductVariants: {Count} variants for product {ProductId}",
                variants.Count, productId);
            return variants.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify GetProductVariants failed: ProductId={ProductId}", productId);
            return Array.Empty<ProductVariantDto>();
        }
    }

    // ─────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────

    private static ShopifyProduct ParseShopifyProduct(JsonElement productEl)
    {
        var product = new ShopifyProduct
        {
            Id = productEl.TryGetProperty("id", out var idEl) ? idEl.GetInt64() : 0,
            Title = productEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null,
            Vendor = productEl.TryGetProperty("vendor", out var vendorEl) ? vendorEl.GetString() : null,
            ProductType = productEl.TryGetProperty("product_type", out var typeEl) ? typeEl.GetString() : null,
            Status = productEl.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null
        };

        if (productEl.TryGetProperty("variants", out var variantsEl))
        {
            foreach (var variantEl in variantsEl.EnumerateArray())
            {
                product.Variants.Add(new ShopifyVariant
                {
                    Id = variantEl.TryGetProperty("id", out var vidEl) ? vidEl.GetInt64() : 0,
                    Sku = variantEl.TryGetProperty("sku", out var skuEl) ? skuEl.GetString() : null,
                    Price = variantEl.TryGetProperty("price", out var priceEl) ? priceEl.GetString() : null,
                    CompareAtPrice = variantEl.TryGetProperty("compare_at_price", out var capEl)
                        ? capEl.GetString()
                        : null,
                    InventoryQuantity = variantEl.TryGetProperty("inventory_quantity", out var iqEl)
                        ? iqEl.GetInt32()
                        : 0,
                    InventoryItemId = variantEl.TryGetProperty("inventory_item_id", out var iiEl)
                        ? iiEl.GetInt64()
                        : 0,
                    Title = variantEl.TryGetProperty("title", out var vtEl) ? vtEl.GetString() : null
                });
            }
        }

        return product;
    }

    // ─────────────────────────────────────────────────────────────────────
    // ISettlementCapableAdapter — Shopify Payments Payouts
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves Shopify Payments settlement (payouts + balance transactions).
    /// GET /admin/api/2024-01/shopify_payments/payouts.json
    /// GET /admin/api/2024-01/shopify_payments/balance/transactions.json
    /// </summary>
    public async Task<SettlementDto?> GetSettlementAsync(
        DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.GetSettlementAsync {Start} - {End}", startDate, endDate);

        try
        {
            var settlement = new SettlementDto
            {
                PlatformCode = PlatformCode,
                StartDate = startDate,
                EndDate = endDate,
                Currency = "USD"
            };

            // Fetch payouts in date range
            var payoutsUrl = $"{BaseUrl}/shopify_payments/payouts.json" +
                $"?date_min={startDate:yyyy-MM-dd}&date_max={endDate:yyyy-MM-dd}&status=paid";

            var payoutsResponse = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, payoutsUrl);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!payoutsResponse.IsSuccessStatusCode)
            {
                var error = await payoutsResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Shopify GetSettlement payouts failed {Status}: {Error}",
                    payoutsResponse.StatusCode, error);
                return null;
            }

            var payoutsContent = await payoutsResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var payoutsDoc = JsonDocument.Parse(payoutsContent);

            if (payoutsDoc.RootElement.TryGetProperty("payouts", out var payouts))
            {
                foreach (var payout in payouts.EnumerateArray())
                {
                    var amount = 0m;
                    if (payout.TryGetProperty("amount", out var amtProp))
                        decimal.TryParse(amtProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out amount);

                    settlement.NetAmount += amount;

                    var payoutId = payout.TryGetProperty("id", out var idProp) ? idProp.GetInt64().ToString() : "unknown";
                    var payoutDate = DateTime.UtcNow;
                    if (payout.TryGetProperty("date", out var dateProp) &&
                        DateTime.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var parsed))
                    {
                        payoutDate = parsed;
                    }

                    settlement.Lines.Add(new SettlementLineDto
                    {
                        OrderNumber = $"payout-{payoutId}",
                        TransactionType = "Payout",
                        Amount = amount,
                        TransactionDate = payoutDate
                    });
                }
            }

            // Fetch balance transactions for detail breakdown
            var txUrl = $"{BaseUrl}/shopify_payments/balance/transactions.json" +
                $"?payout_status=paid&test=false";

            var txResponse = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, txUrl);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (txResponse.IsSuccessStatusCode)
            {
                var txContent = await txResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var txDoc = JsonDocument.Parse(txContent);

                if (txDoc.RootElement.TryGetProperty("transactions", out var transactions))
                {
                    foreach (var tx in transactions.EnumerateArray())
                    {
                        var txAmount = 0m;
                        if (tx.TryGetProperty("amount", out var txAmtProp))
                            decimal.TryParse(txAmtProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out txAmount);

                        var fee = 0m;
                        if (tx.TryGetProperty("fee", out var feeProp))
                            decimal.TryParse(feeProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out fee);

                        var txType = tx.TryGetProperty("type", out var typeProp)
                            ? typeProp.GetString() ?? "sale"
                            : "sale";

                        if (string.Equals(txType, "charge", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(txType, "sale", StringComparison.OrdinalIgnoreCase))
                        {
                            settlement.TotalSales += txAmount;
                        }
                        else if (string.Equals(txType, "refund", StringComparison.OrdinalIgnoreCase))
                        {
                            settlement.TotalReturnDeduction += Math.Abs(txAmount);
                        }

                        settlement.TotalCommission += Math.Abs(fee);
                    }
                }
            }

            settlement.TotalShippingCost = 0; // Shopify Payments does not break out shipping cost separately
            _logger.LogInformation("Shopify GetSettlement OK — Net={Net}, Sales={Sales}, Commission={Commission}",
                settlement.NetAmount, settlement.TotalSales, settlement.TotalCommission);
            return settlement;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Shopify GetSettlement failed");
            return null;
        }
    }

    /// <summary>
    /// Shopify does not have a dedicated cargo invoice API.
    /// Returns empty list — shipping costs are included in balance transactions.
    /// </summary>
    public Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(
        DateTime startDate, CancellationToken ct = default)
    {
        _logger.LogInformation("ShopifyAdapter.GetCargoInvoicesAsync — Shopify does not provide cargo invoices separately");
        return Task.FromResult<IReadOnlyList<CargoInvoiceDto>>(Array.Empty<CargoInvoiceDto>());
    }

    // ─────────────────────────────────────────────────────────────────────
    // IClaimCapableAdapter — Shopify Refunds (orders with financial_status=refunded)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pulls refunded orders from Shopify. Shopify uses "refunds" rather than "claims".
    /// GET /admin/api/2024-01/orders.json?financial_status=refunded
    /// </summary>
    public async Task<IReadOnlyList<ExternalClaimDto>> PullClaimsAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.PullClaimsAsync since={Since}", since);

        var claims = new List<ExternalClaimDto>();

        try
        {
            var sinceDate = since ?? DateTime.UtcNow.AddDays(-30);
            var url = $"{BaseUrl}/orders.json?financial_status=refunded" +
                $"&updated_at_min={sinceDate:yyyy-MM-ddTHH:mm:ssZ}&status=any&limit=250";

            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var request = CreateAuthenticatedRequest(HttpMethod.Get, url);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Shopify PullClaims failed {Status}: {Error}",
                    response.StatusCode, error);
                return claims.AsReadOnly();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("orders", out var orders))
                return claims.AsReadOnly();

            foreach (var order in orders.EnumerateArray())
            {
                var orderId = order.TryGetProperty("id", out var idProp) ? idProp.GetInt64().ToString() : string.Empty;
                var orderNumber = order.TryGetProperty("order_number", out var onProp) ? onProp.GetInt64().ToString() : orderId;

                var totalRefund = 0m;
                if (order.TryGetProperty("refunds", out var refunds))
                {
                    foreach (var refund in refunds.EnumerateArray())
                    {
                        var refundId = refund.TryGetProperty("id", out var ridProp) ? ridProp.GetInt64().ToString() : orderId;

                        var claim = new ExternalClaimDto
                        {
                            PlatformClaimId = refundId,
                            PlatformCode = PlatformCode,
                            OrderNumber = orderNumber,
                            Status = "refunded",
                            Reason = refund.TryGetProperty("note", out var noteProp)
                                ? noteProp.GetString() ?? "Customer refund"
                                : "Customer refund",
                            Currency = order.TryGetProperty("currency", out var curProp)
                                ? curProp.GetString() ?? "USD"
                                : "USD"
                        };

                        if (refund.TryGetProperty("created_at", out var dateProp) &&
                            DateTime.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture,
                                DateTimeStyles.RoundtripKind, out var claimDate))
                        {
                            claim.ClaimDate = claimDate;
                        }
                        else
                        {
                            claim.ClaimDate = DateTime.UtcNow;
                        }

                        // Extract customer info
                        if (order.TryGetProperty("customer", out var customer))
                        {
                            var firstName = customer.TryGetProperty("first_name", out var fnProp) ? fnProp.GetString() ?? "" : "";
                            var lastName = customer.TryGetProperty("last_name", out var lnProp) ? lnProp.GetString() ?? "" : "";
                            claim.CustomerName = $"{firstName} {lastName}".Trim();
                            claim.CustomerEmail = customer.TryGetProperty("email", out var emProp) ? emProp.GetString() : null;
                        }

                        // Extract refund line items
                        if (refund.TryGetProperty("refund_line_items", out var refundLines))
                        {
                            foreach (var rl in refundLines.EnumerateArray())
                            {
                                var qty = rl.TryGetProperty("quantity", out var qtyProp) ? qtyProp.GetInt32() : 1;
                                var subtotal = 0m;
                                if (rl.TryGetProperty("subtotal", out var stProp))
                                    decimal.TryParse(stProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out subtotal);

                                var lineItem = rl.TryGetProperty("line_item", out var liProp) ? liProp : default;
                                var sku = lineItem.ValueKind == JsonValueKind.Object &&
                                    lineItem.TryGetProperty("sku", out var skuProp) ? skuProp.GetString() : null;
                                var productName = lineItem.ValueKind == JsonValueKind.Object &&
                                    lineItem.TryGetProperty("title", out var titleProp)
                                    ? titleProp.GetString() ?? string.Empty
                                    : string.Empty;

                                claim.Lines.Add(new ExternalClaimLineDto
                                {
                                    SKU = sku,
                                    ProductName = productName,
                                    Quantity = qty,
                                    UnitPrice = qty > 0 ? subtotal / qty : subtotal
                                });

                                totalRefund += subtotal;
                            }
                        }

                        claim.Amount = totalRefund;
                        claims.Add(claim);
                    }
                }
            }

            _logger.LogInformation("Shopify PullClaims: {Count} refunds fetched", claims.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Shopify PullClaims failed");
        }

        return claims.AsReadOnly();
    }

    /// <summary>
    /// Approves a refund on Shopify by creating a refund entry.
    /// POST /admin/api/2024-01/orders/{orderId}/refunds.json
    /// Note: claimId is expected to be the order ID.
    /// </summary>
    public async Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ShopifyAdapter.ApproveClaimAsync ClaimId={ClaimId}", claimId);

        try
        {
            // Shopify refund approval = calculate + create refund
            var calcUrl = $"{BaseUrl}/orders/{claimId}/refunds/calculate.json";
            var calcPayload = new { refund = new { shipping = new { full_refund = true } } };
            var calcJson = JsonSerializer.Serialize(calcPayload, _jsonOptions);

            var calcResponse = await ThrottledExecuteAsync(async token =>
            {
                using var request = CreateAuthenticatedRequest(HttpMethod.Post, calcUrl,
                    new StringContent(calcJson, Encoding.UTF8, "application/json"));
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (!calcResponse.IsSuccessStatusCode)
            {
                var error = await calcResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Shopify ApproveClaim calculate failed {Status}: {Error}",
                    calcResponse.StatusCode, error);
                return false;
            }

            // Create refund
            var refundUrl = $"{BaseUrl}/orders/{claimId}/refunds.json";
            var refundPayload = new { refund = new { notify = true } };
            var refundJson = JsonSerializer.Serialize(refundPayload, _jsonOptions);

            var refundResponse = await ThrottledExecuteAsync(async token =>
            {
                using var request = CreateAuthenticatedRequest(HttpMethod.Post, refundUrl,
                    new StringContent(refundJson, Encoding.UTF8, "application/json"));
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (refundResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Shopify ApproveClaim OK — ClaimId={ClaimId}", claimId);
                return true;
            }

            var refundError = await refundResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Shopify ApproveClaim create refund failed {Status}: {Error}",
                refundResponse.StatusCode, refundError);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Shopify ApproveClaim failed — ClaimId={ClaimId}", claimId);
            return false;
        }
    }

    /// <summary>
    /// Shopify processes refunds automatically — manual rejection is not supported.
    /// Always returns false.
    /// </summary>
    public Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "ShopifyAdapter.RejectClaimAsync — Shopify auto-processes refunds, manual rejection not supported. ClaimId={ClaimId}",
            claimId);
        return Task.FromResult(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    // IInvoiceCapableAdapter — Shopify has no invoice API
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shopify does not have a native invoice API.
    /// Logs the request and returns true (invoice handled externally).
    /// </summary>
    public Task<bool> SendInvoiceLinkAsync(
        string shipmentPackageId, string invoiceUrl, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ShopifyAdapter.SendInvoiceLinkAsync — Shopify has no invoice API, logged. PackageId={PackageId}, Url={Url}",
            shipmentPackageId, invoiceUrl);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Shopify does not have a native invoice file upload API.
    /// Logs the request and returns true (invoice handled externally).
    /// </summary>
    public Task<bool> SendInvoiceFileAsync(
        string shipmentPackageId, byte[] pdfBytes, string fileName, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ShopifyAdapter.SendInvoiceFileAsync — Shopify has no invoice API, logged. PackageId={PackageId}, File={FileName}, Size={Size}",
            shipmentPackageId, fileName, pdfBytes?.Length ?? 0);
        return Task.FromResult(true);
    }

    // ── IPingableAdapter ──
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            if (_httpClient.BaseAddress is null) return false;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var resp = await _httpClient.GetAsync(_httpClient.BaseAddress, cts.Token).ConfigureAwait(false);
            return (int)resp.StatusCode < 500;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Shopify ping failed"); return false; }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Configuration options
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Configuration options for ShopifyAdapter.
/// Bind from appsettings.json section "Integrations:Shopify".
/// </summary>
public sealed class ShopifyOptions
{
    /// <summary>Section key in appsettings.json.</summary>
    public const string Section = "Integrations:Shopify";

    /// <summary>Shopify shop domain, e.g. "my-store.myshopify.com".</summary>
    public string ShopDomain { get; set; } = string.Empty;

    /// <summary>Private app or custom app access token.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Shopify location ID for inventory operations (numeric string).</summary>
    public string LocationId { get; set; } = string.Empty;

    /// <summary>Client secret used for HMAC-SHA256 webhook verification (IRON RULE).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Whether the Shopify integration is enabled.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>HTTP client timeout in seconds.</summary>
    public int HttpTimeoutSeconds { get; set; } = 30;
}

// ─────────────────────────────────────────────────────────────────────────────
// Internal response models (Shopify REST API 2024-01)
// ─────────────────────────────────────────────────────────────────────────────

internal sealed class ShopifyProductsResponse
{
    [JsonPropertyName("products")]
    public List<ShopifyProduct> Products { get; set; } = new();
}

internal sealed class ShopifyProduct
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("vendor")]
    public string? Vendor { get; set; }

    [JsonPropertyName("product_type")]
    public string? ProductType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("variants")]
    public List<ShopifyVariant> Variants { get; set; } = new();
}

internal sealed class ShopifyVariant
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("compare_at_price")]
    public string? CompareAtPrice { get; set; }

    [JsonPropertyName("inventory_quantity")]
    public int InventoryQuantity { get; set; }

    [JsonPropertyName("inventory_item_id")]
    public long InventoryItemId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

internal sealed class ShopifyVariantsResponse
{
    [JsonPropertyName("variants")]
    public List<ShopifyVariant> Variants { get; set; } = new();
}

internal sealed class ShopifyOrdersResponse
{
    [JsonPropertyName("orders")]
    public List<ShopifyOrder> Orders { get; set; } = new();
}

internal sealed class ShopifyOrder
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("financial_status")]
    public string? FinancialStatus { get; set; }

    [JsonPropertyName("fulfillment_status")]
    public string? FulfillmentStatus { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("total_price")]
    public string? TotalPrice { get; set; }

    [JsonPropertyName("total_discounts")]
    public string? TotalDiscounts { get; set; }

    [JsonPropertyName("customer")]
    public ShopifyCustomer? Customer { get; set; }

    [JsonPropertyName("line_items")]
    public List<ShopifyOrderLineItem> LineItems { get; set; } = new();
}

internal sealed class ShopifyCustomer
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

internal sealed class ShopifyOrderLineItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("total_discount")]
    public string? TotalDiscount { get; set; }
}
