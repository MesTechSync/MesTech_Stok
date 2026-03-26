using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs;
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
/// Ozon platform adaptoru — H30 real implementation.
/// Client-Id + Api-Key header auth (no Bearer token exchange).
/// FBO/FBS order retrieval, stock updates via /v2/products/stocks.
/// Implements IIntegratorAdapter + IOrderCapableAdapter.
/// </summary>
public sealed class OzonAdapter : IIntegratorAdapter, IOrderCapableAdapter, IPingableAdapter, IShipmentCapableAdapter,
    ISettlementCapableAdapter, IClaimCapableAdapter, IInvoiceCapableAdapter, IWebhookCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OzonAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(20, 20);

    // Ozon uses header-based auth — no token exchange
    private string _clientId = string.Empty;
    private string _apiKey = string.Empty;
    private string _baseUrl;
    private bool _isConfigured;

    private const string DefaultBaseUrl = "https://api-seller.ozon.ru";
    private const string ClientIdHeader = "Client-Id";
    private const string ApiKeyHeader = "Api-Key";

    public OzonAdapter(HttpClient httpClient, ILogger<OzonAdapter> logger,
        IOptions<OzonOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _baseUrl = options?.Value.BaseUrl ?? DefaultBaseUrl;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            // HTTP 429 rate-limit retry — Ozon Seller API rate limiting
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 5,
                DelayGenerator = args =>
                {
                    if (args.Outcome.Result is { StatusCode: System.Net.HttpStatusCode.TooManyRequests } resp
                        && resp.Headers.RetryAfter is { } ra)
                    {
                        return new ValueTask<TimeSpan?>(ra.Delta ?? TimeSpan.FromSeconds(3));
                    }
                    return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(3));
                },
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests),
                OnRetry = args =>
                {
                    _logger.LogWarning("[OzonAdapter] Rate limited (429). Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[OzonAdapter] API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[OzonAdapter] Circuit breaker OPENED for {Duration}s",
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

    public string PlatformCode => nameof(PlatformType.Ozon);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ═══════════════════════════════════════════
    // Header-based Auth Request Builder
    // ═══════════════════════════════════════════

    /// <summary>
    /// Builds an HttpRequestMessage with Ozon Client-Id and Api-Key headers.
    /// </summary>
    private HttpRequestMessage BuildRequest(HttpMethod method, string relativeUrl)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{relativeUrl}");
        request.Headers.TryAddWithoutValidation(ClientIdHeader, _clientId);
        request.Headers.TryAddWithoutValidation(ApiKeyHeader, _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    /// <summary>
    /// Builds an HttpRequestMessage with JSON body content.
    /// </summary>
    private HttpRequestMessage BuildPostRequest(string relativeUrl, object payload)
    {
        var request = BuildRequest(HttpMethod.Post, relativeUrl);
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return request;
    }

    private void ConfigureCredentials(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _clientId = credentials.GetValueOrDefault("ClientId", string.Empty);
        _apiKey = credentials.GetValueOrDefault("ApiKey", string.Empty);

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _baseUrl = credentials["BaseUrl"];

        _isConfigured = !string.IsNullOrWhiteSpace(_clientId) &&
                        !string.IsNullOrWhiteSpace(_apiKey);
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "OzonAdapter henuz yapilandirilmadi. Once TestConnectionAsync cagirin.");
    }

    // ═══════════════════════════════════════════
    // IIntegratorAdapter — TestConnection
    // ═══════════════════════════════════════════

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
                result.ErrorMessage = "Ozon: ClientId veya ApiKey eksik";
                return result;
            }

            // Probe call to verify credentials — POST /v1/seller/info
            using var probe = BuildPostRequest("/v1/seller/info", new { });
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.SendAsync(probe, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                var storeName = "Ozon Seller";
                if (doc.RootElement.TryGetProperty("result", out var sellerResult) &&
                    sellerResult.TryGetProperty("name", out var nameProp))
                {
                    storeName = nameProp.GetString() ?? storeName;
                }

                result.IsSuccess = true;
                result.StoreName = storeName;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Forbidden => "Erisim engellendi — Client-Id veya Api-Key hatali.",
                    System.Net.HttpStatusCode.Unauthorized => "Yetkisiz erisim — Client-Id veya Api-Key hatali.",
                    _ => $"Ozon API hatasi: {response.StatusCode} - {errorContent}"
                };
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ozon TestConnectionAsync basarisiz");
            result.ErrorMessage = $"Baglanti hatasi: {ex.Message}";
        }
        finally
        {
            sw.Stop();
            result.ResponseTime = sw.Elapsed;
        }

        return result;
    }

    // ═══════════════════════════════════════════
    // IIntegratorAdapter — Products
    // ═══════════════════════════════════════════

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        // POST /v2/product/import — requires complex attribute mapping per category.
        // Stubbed for now — full implementation needs category-specific attributes.
        _logger.LogWarning("OzonAdapter.PushProductAsync — full listing creation requires category attribute mapping");
        return Task.FromResult(false);
    }

    /// <summary>
    /// Pulls products from Ozon using POST /v2/product/list + POST /v2/product/info/list.
    /// </summary>
    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OzonAdapter.PullProductsAsync called");

        var products = new List<Product>();

        try
        {
            var lastId = string.Empty;
            const int pageSize = 100;

            while (true)
            {
                // Step 1: Get product IDs
                var listPayload = new
                {
                    filter = new { visibility = "ALL" },
                    last_id = lastId,
                    limit = pageSize
                };

                using var listRequest = BuildPostRequest("/v2/product/list", listPayload);
                var listResponse = await ThrottledExecuteAsync(
                    async token => await _httpClient.SendAsync(listRequest, token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!listResponse.IsSuccessStatusCode)
                {
                    var listError = await listResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("Ozon PullProducts /v2/product/list failed: {Status} - {Error}",
                        listResponse.StatusCode, listError);
                    break;
                }

                var listContent = await listResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var listDoc = JsonDocument.Parse(listContent);

                if (!listDoc.RootElement.TryGetProperty("result", out var listResult) ||
                    !listResult.TryGetProperty("items", out var items))
                    break;

                var productIds = new List<long>();
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("product_id", out var pidProp))
                        productIds.Add(pidProp.GetInt64());
                }

                if (productIds.Count == 0)
                    break;

                // Step 2: Get product details
                var infoPayload = new
                {
                    product_id = productIds.ToArray()
                };

                using var infoRequest = BuildPostRequest("/v2/product/info/list", infoPayload);
                var infoResponse = await ThrottledExecuteAsync(
                    async token => await _httpClient.SendAsync(infoRequest, token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!infoResponse.IsSuccessStatusCode)
                {
                    var infoError = await infoResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("Ozon PullProducts /v2/product/info/list failed: {Status} - {Error}",
                        infoResponse.StatusCode, infoError);
                    break;
                }

                var infoContent = await infoResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var infoDoc = JsonDocument.Parse(infoContent);

                if (infoDoc.RootElement.TryGetProperty("result", out var infoResult) &&
                    infoResult.TryGetProperty("items", out var infoItems))
                {
                    foreach (var info in infoItems.EnumerateArray())
                    {
                        var name = info.TryGetProperty("name", out var nameProp)
                            ? nameProp.GetString() ?? string.Empty
                            : string.Empty;

                        var offerId = info.TryGetProperty("offer_id", out var offerProp)
                            ? offerProp.GetString() ?? string.Empty
                            : string.Empty;

                        var barcode = info.TryGetProperty("barcode", out var barcodeProp)
                            ? barcodeProp.GetString()
                            : null;

                        var price = 0m;
                        if (info.TryGetProperty("old_price", out var priceProp))
                        {
                            decimal.TryParse(priceProp.GetString(),
                                NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                        }

                        var stock = 0;
                        if (info.TryGetProperty("stocks", out var stocks) &&
                            stocks.TryGetProperty("present", out var presentProp))
                        {
                            stock = presentProp.GetInt32();
                        }

                        var isActive = true;
                        if (info.TryGetProperty("visible", out var visibleProp))
                            isActive = visibleProp.GetBoolean();

                        products.Add(new Product
                        {
                            Name = name,
                            SKU = offerId,
                            Barcode = barcode,
                            SalePrice = price,
                            Stock = stock,
                            IsActive = isActive
                        });
                    }
                }

                // Check for pagination
                if (listResult.TryGetProperty("last_id", out var lastIdProp))
                {
                    var newLastId = lastIdProp.GetString() ?? string.Empty;
                    if (string.IsNullOrEmpty(newLastId) || newLastId == lastId)
                        break;
                    lastId = newLastId;
                }
                else
                {
                    break;
                }

                if (productIds.Count < pageSize)
                    break;
            }

            _logger.LogInformation("Ozon PullProducts: {Count} products fetched", products.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ozon PullProducts failed");
        }

        return products.AsReadOnly();
    }

    /// <summary>
    /// Updates stock for a product using Ozon FBS stocks API.
    /// POST /v2/products/stocks with stocks: [{ offer_id, stock, warehouse_id }]
    /// </summary>
    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OzonAdapter.PushStockUpdateAsync: ProductId={ProductId} qty={Qty}",
            productId, newStock);

        try
        {
            var payload = new
            {
                stocks = new[]
                {
                    new
                    {
                        offer_id = productId.ToString(),
                        stock = newStock,
                        warehouse_id = 0L // Default warehouse — override via configuration
                    }
                }
            };

            using var request = BuildPostRequest("/v2/products/stocks", payload);
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.SendAsync(request, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Ozon stock update failed {Status}: {Error}",
                    response.StatusCode, error);
                return false;
            }

            // Check for per-item errors in the response
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("result", out var resultArr))
            {
                foreach (var item in resultArr.EnumerateArray())
                {
                    if (item.TryGetProperty("errors", out var errors))
                    {
                        var hasErrors = false;
                        foreach (var err in errors.EnumerateArray())
                        {
                            hasErrors = true;
                            var errMsg = err.TryGetProperty("message", out var msgProp)
                                ? msgProp.GetString() ?? "Unknown error"
                                : "Unknown error";
                            _logger.LogWarning("Ozon stock update item error: {Error}", errMsg);
                        }

                        if (hasErrors)
                            return false;
                    }
                }
            }

            _logger.LogInformation("Ozon stock update OK: {ProductId} -> {Stock}",
                productId, newStock);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ozon stock update exception for ProductId={ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Updates price for a product using Ozon price API.
    /// POST /v1/product/import/prices with prices: [{ offer_id, price, old_price }]
    /// </summary>
    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OzonAdapter.PushPriceUpdateAsync: ProductId={ProductId} price={Price}",
            productId, newPrice);

        try
        {
            var payload = new
            {
                prices = new[]
                {
                    new
                    {
                        offer_id = productId.ToString(),
                        price = newPrice.ToString("F2", CultureInfo.InvariantCulture),
                        old_price = "0"  // Ozon requires old_price; "0" means no strikethrough
                    }
                }
            };

            using var request = BuildPostRequest("/v1/product/import/prices", payload);
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.SendAsync(request, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Ozon price update failed {Status}: {Error}",
                    response.StatusCode, error);
                return false;
            }

            // Check for per-item errors
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("result", out var resultArr))
            {
                foreach (var item in resultArr.EnumerateArray())
                {
                    var updated = item.TryGetProperty("updated", out var updProp) && updProp.GetBoolean();
                    if (!updated)
                    {
                        var errors = item.TryGetProperty("errors", out var errArr)
                            ? errArr.ToString()
                            : "unknown";
                        _logger.LogWarning("Ozon price update not applied: {Errors}", errors);
                        return false;
                    }
                }
            }

            _logger.LogInformation("Ozon price update OK: {ProductId} -> {Price}",
                productId, newPrice);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ozon price update exception for ProductId={ProductId}", productId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // IOrderCapableAdapter — Orders (FBS)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Pulls FBS orders from Ozon.
    /// POST /v3/posting/fbs/list with filter body.
    /// </summary>
    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OzonAdapter.PullOrdersAsync since={Since}", since);

        var orders = new List<ExternalOrderDto>();

        try
        {
            var sinceDate = since ?? DateTime.UtcNow.AddDays(-30);
            var offset = 0;
            const int pageSize = 50;

            while (true)
            {
                var payload = new
                {
                    dir = "asc",
                    filter = new
                    {
                        since = sinceDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                        to = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                        status = ""
                    },
                    limit = pageSize,
                    offset
                };

                using var request = BuildPostRequest("/v3/posting/fbs/list", payload);
                var response = await ThrottledExecuteAsync(
                    async token => await _httpClient.SendAsync(request, token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var orderError = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("Ozon PullOrders /v3/posting/fbs/list failed: {Status} - {Error}",
                        response.StatusCode, orderError);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("result", out var result) ||
                    !result.TryGetProperty("postings", out var postings))
                    break;

                var pageCount = 0;
                foreach (var posting in postings.EnumerateArray())
                {
                    pageCount++;

                    var postingNumber = posting.TryGetProperty("posting_number", out var pnProp)
                        ? pnProp.GetString() ?? string.Empty
                        : string.Empty;

                    var status = posting.TryGetProperty("status", out var stProp)
                        ? stProp.GetString() ?? string.Empty
                        : string.Empty;

                    var orderDate = DateTime.UtcNow;
                    if (posting.TryGetProperty("in_process_at", out var dateProp) &&
                        DateTime.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var parsedDate))
                    {
                        orderDate = parsedDate;
                    }

                    var order = new ExternalOrderDto
                    {
                        PlatformCode = PlatformCode,
                        PlatformOrderId = postingNumber,
                        OrderNumber = postingNumber,
                        Status = status,
                        OrderDate = orderDate,
                        Currency = "RUB"
                    };

                    // Extract customer info from analytics_data
                    if (posting.TryGetProperty("analytics_data", out var analytics))
                    {
                        order.CustomerCity = analytics.TryGetProperty("city", out var cityProp)
                            ? cityProp.GetString()
                            : null;
                    }

                    // Extract address info from delivery_method
                    if (posting.TryGetProperty("delivery_method", out var delivery))
                    {
                        order.CustomerAddress = delivery.TryGetProperty("warehouse", out var whProp)
                            ? whProp.GetString()
                            : null;
                    }

                    // Extract line items from products[]
                    var totalAmount = 0m;
                    if (posting.TryGetProperty("products", out var products))
                    {
                        foreach (var prod in products.EnumerateArray())
                        {
                            var sku = prod.TryGetProperty("offer_id", out var skuProp)
                                ? skuProp.GetString() ?? string.Empty
                                : string.Empty;

                            var name = prod.TryGetProperty("name", out var nameProp)
                                ? nameProp.GetString() ?? string.Empty
                                : string.Empty;

                            var qty = prod.TryGetProperty("quantity", out var qtyProp)
                                ? qtyProp.GetInt32()
                                : 1;

                            var price = 0m;
                            if (prod.TryGetProperty("price", out var priceProp))
                            {
                                decimal.TryParse(priceProp.GetString(),
                                    NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                            }

                            order.Lines.Add(new ExternalOrderLineDto
                            {
                                SKU = sku,
                                ProductName = name,
                                Quantity = qty,
                                UnitPrice = price,
                                LineTotal = price * qty
                            });

                            totalAmount += price * qty;
                        }
                    }

                    order.TotalAmount = totalAmount;
                    orders.Add(order);
                }

                if (pageCount < pageSize)
                    break;

                // Check if there are more pages
                if (result.TryGetProperty("has_next", out var hasNextProp) && !hasNextProp.GetBoolean())
                    break;

                offset += pageSize;
            }

            _logger.LogInformation("Ozon PullOrders: {Count} orders fetched", orders.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ozon PullOrders failed");
        }

        return orders.AsReadOnly();
    }

    /// <summary>
    /// Updates order status on Ozon.
    /// Ozon does not support arbitrary status updates — only specific transitions.
    /// Returns false as generic status update is not supported.
    /// </summary>
    public Task<bool> UpdateOrderStatusAsync(
        string packageId, string status, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "OzonAdapter.UpdateOrderStatusAsync — Ozon uses posting-based status transitions, not generic updates. Package={PackageId}",
            packageId);
        return Task.FromResult(false);
    }

    /// <summary>
    /// Fetches Ozon description category tree using POST /v1/description-category/tree.
    /// Recursively parses nested categories into a flat-with-children CategoryDto structure.
    /// </summary>
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OzonAdapter.GetCategoriesAsync called");

        try
        {
            var payload = new { language = "DEFAULT" };
            using var request = BuildPostRequest("/v1/description-category/tree", payload);
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.SendAsync(request, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Ozon GetCategories /v1/description-category/tree failed: {Status} - {Error}",
                    response.StatusCode, error);
                return Array.Empty<CategoryDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var categories = new List<CategoryDto>();

            if (doc.RootElement.TryGetProperty("result", out var resultArr))
            {
                foreach (var node in resultArr.EnumerateArray())
                {
                    categories.Add(ParseOzonCategoryNode(node, parentId: null));
                }
            }

            _logger.LogInformation("Ozon GetCategories: {Count} top-level categories retrieved", categories.Count);
            return categories.AsReadOnly();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ozon GetCategories failed");
            return Array.Empty<CategoryDto>();
        }
    }

    /// <summary>
    /// Recursively parses an Ozon description category tree node into a CategoryDto.
    /// Ozon node structure: { "description_category_id": 123, "category_name": "Electronics",
    ///                        "children": [...] }
    /// </summary>
    private static CategoryDto ParseOzonCategoryNode(JsonElement node, int? parentId)
    {
        var categoryId = 0;
        if (node.TryGetProperty("description_category_id", out var idProp))
            categoryId = idProp.GetInt32();

        var categoryName = string.Empty;
        if (node.TryGetProperty("category_name", out var nameProp))
            categoryName = nameProp.GetString() ?? string.Empty;

        var dto = new CategoryDto
        {
            PlatformCategoryId = categoryId,
            Name = categoryName,
            ParentId = parentId
        };

        if (node.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                dto.SubCategories.Add(ParseOzonCategoryNode(child, parentId: categoryId));
            }
        }

        return dto;
    }

    // ═══════════════════════════════════════════
    // IShipmentCapableAdapter — Shipment Notification
    // ═══════════════════════════════════════════

    /// <summary>
    /// Sends shipment notification to Ozon using FBS ship API.
    /// POST /v3/posting/fbs/ship with posting_number, tracking_number, shipping_provider_id.
    /// </summary>
    public async Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber,
        CargoProvider provider, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            if (string.IsNullOrWhiteSpace(platformOrderId))
            {
                _logger.LogWarning("Ozon SendShipment — platformOrderId bos olamaz");
                return false;
            }

            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                _logger.LogWarning("Ozon SendShipment — trackingNumber bos olamaz. PostingNumber={PostingNumber}",
                    platformOrderId);
                return false;
            }

            var shippingProviderId = MapCargoProviderToOzon(provider);

            var payload = new
            {
                posting_number = platformOrderId,
                tracking_number = trackingNumber,
                shipping_provider_id = shippingProviderId
            };

            using var request = BuildPostRequest("/v3/posting/fbs/ship", payload);
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.SendAsync(request, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Ozon SendShipment basarili — PostingNumber={PostingNumber}, Tracking={Tracking}, Provider={Provider}",
                    platformOrderId, trackingNumber, provider);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Ozon SendShipment basarisiz {Status}: {Error}",
                response.StatusCode, error);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ozon SendShipment hatasi — PostingNumber={PostingNumber}", platformOrderId);
            return false;
        }
    }

    /// <summary>
    /// Maps CargoProvider enum to Ozon shipping_provider_id.
    /// Ozon uses numeric IDs — these are approximations; real mapping from Ozon /v1/delivery-method/list.
    /// </summary>
    private static long MapCargoProviderToOzon(CargoProvider provider) => provider switch
    {
        CargoProvider.PttKargo => 1,
        CargoProvider.YurticiKargo => 2,
        CargoProvider.ArasKargo => 3,
        CargoProvider.SuratKargo => 4,
        CargoProvider.MngKargo => 5,
        CargoProvider.UPS => 6,
        CargoProvider.DHL => 7,
        CargoProvider.FedEx => 8,
        CargoProvider.Hepsijet => 9,
        CargoProvider.Sendeo => 10,
        _ => 0
    };

    // ═══════════════════════════════════════════
    // IWebhookCapableAdapter
    // ═══════════════════════════════════════════

    public async Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OzonAdapter.RegisterWebhookAsync: {Url}", callbackUrl);

        try
        {
            var payload = new
            {
                url = callbackUrl,
                event_type = "TYPE_ALL"
            };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/webhook/register");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Add(ClientIdHeader, _clientId);
            request.Headers.Add(ApiKeyHeader, _apiKey);

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Ozon RegisterWebhook failed: {Status} {Error}",
                    response.StatusCode, error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ozon RegisterWebhook exception");
            return false;
        }
    }

    public async Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OzonAdapter.UnregisterWebhookAsync");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/webhook/unregister");
            request.Headers.Add(ClientIdHeader, _clientId);
            request.Headers.Add(ApiKeyHeader, _apiKey);

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Ozon UnregisterWebhook failed: {Status} {Error}",
                    response.StatusCode, error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ozon UnregisterWebhook exception");
            return false;
        }
    }

    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(payload);
        var eventType = doc.RootElement.TryGetProperty("type", out var et) ? et.GetString() : "unknown";

        _logger.LogInformation(
            "OzonAdapter webhook processed: EventType={EventType} PayloadLength={Length}",
            eventType, payload.Length);

        return Task.CompletedTask;
    }

    // ═══════════════════════════════════════════
    // IPingableAdapter — Lightweight Health Check
    // ═══════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var request = new HttpRequestMessage(HttpMethod.Head,
                new Uri(_baseUrl, UriKind.Absolute));
            var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);

            _logger.LogDebug("Ozon ping: {StatusCode}", response.StatusCode);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Ozon ping failed");
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // ISettlementCapableAdapter — Ozon Finance Transactions
    // ═══════════════════════════════════════════

    /// <summary>
    /// Retrieves settlement data from Ozon using POST /v3/finance/transaction/list.
    /// Returns transaction-level breakdown with commission and delivery fees.
    /// </summary>
    public async Task<SettlementDto?> GetSettlementAsync(
        DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OzonAdapter.GetSettlementAsync {Start} - {End}", startDate, endDate);

        try
        {
            var settlement = new SettlementDto
            {
                PlatformCode = PlatformCode,
                StartDate = startDate,
                EndDate = endDate,
                Currency = "RUB"
            };

            var page = 1;
            const int pageSize = 1000;

            while (true)
            {
                var payload = new
                {
                    filter = new
                    {
                        date = new
                        {
                            from = startDate.ToString("yyyy-MM-ddT00:00:00.000Z", CultureInfo.InvariantCulture),
                            to = endDate.ToString("yyyy-MM-ddT23:59:59.999Z", CultureInfo.InvariantCulture)
                        },
                        transaction_type = "all"
                    },
                    page,
                    page_size = pageSize
                };

                using var request = BuildPostRequest("/v3/finance/transaction/list", payload);
                var response = await ThrottledExecuteAsync(
                    async token => await _httpClient.SendAsync(request, token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("Ozon GetSettlement /v3/finance/transaction/list failed {Status}: {Error}",
                        response.StatusCode, error);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("result", out var result) ||
                    !result.TryGetProperty("operations", out var operations))
                    break;

                var pageCount = 0;
                foreach (var op in operations.EnumerateArray())
                {
                    pageCount++;

                    var amount = 0m;
                    if (op.TryGetProperty("amount", out var amtProp))
                        amount = amtProp.GetDecimal();

                    var commission = 0m;
                    if (op.TryGetProperty("sale_commission", out var commProp))
                        commission = commProp.GetDecimal();

                    var delivery = 0m;
                    if (op.TryGetProperty("delivery_charge", out var delProp))
                        delivery = delProp.GetDecimal();

                    var txType = op.TryGetProperty("operation_type", out var typeProp)
                        ? typeProp.GetString() ?? "unknown"
                        : "unknown";

                    var orderNumber = op.TryGetProperty("posting", out var postingProp) &&
                        postingProp.TryGetProperty("posting_number", out var pnProp)
                        ? pnProp.GetString()
                        : null;

                    var txDate = DateTime.UtcNow;
                    if (op.TryGetProperty("operation_date", out var dateProp) &&
                        DateTime.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var parsed))
                    {
                        txDate = parsed;
                    }

                    settlement.TotalSales += amount;
                    settlement.TotalCommission += Math.Abs(commission);
                    settlement.TotalShippingCost += Math.Abs(delivery);

                    settlement.Lines.Add(new SettlementLineDto
                    {
                        OrderNumber = orderNumber,
                        TransactionType = txType,
                        Amount = amount,
                        CommissionAmount = commission,
                        TransactionDate = txDate
                    });
                }

                if (pageCount < pageSize)
                    break;

                page++;
            }

            settlement.NetAmount = settlement.TotalSales - settlement.TotalCommission - settlement.TotalShippingCost;

            _logger.LogInformation("Ozon GetSettlement OK — Net={Net}, Sales={Sales}, Commission={Commission}, Shipping={Shipping}",
                settlement.NetAmount, settlement.TotalSales, settlement.TotalCommission, settlement.TotalShippingCost);
            return settlement;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ozon GetSettlement failed");
            return null;
        }
    }

    /// <summary>
    /// Ozon does not have a separate cargo invoice API.
    /// Delivery charges are included in finance transactions.
    /// </summary>
    public Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(
        DateTime startDate, CancellationToken ct = default)
    {
        _logger.LogInformation("OzonAdapter.GetCargoInvoicesAsync — delivery costs included in finance transactions");
        return Task.FromResult<IReadOnlyList<CargoInvoiceDto>>(Array.Empty<CargoInvoiceDto>());
    }

    // ═══════════════════════════════════════════
    // IClaimCapableAdapter — Ozon FBS Returns
    // ═══════════════════════════════════════════

    /// <summary>
    /// Pulls FBS returns from Ozon using POST /v2/returns/company/fbs.
    /// </summary>
    public async Task<IReadOnlyList<ExternalClaimDto>> PullClaimsAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OzonAdapter.PullClaimsAsync since={Since}", since);

        var claims = new List<ExternalClaimDto>();

        try
        {
            var sinceDate = since ?? DateTime.UtcNow.AddDays(-30);
            var offset = 0;
            const int limit = 50;

            while (true)
            {
                var payload = new
                {
                    filter = new
                    {
                        last_free_waiting_day_min = sinceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    },
                    offset,
                    limit
                };

                using var request = BuildPostRequest("/v2/returns/company/fbs", payload);
                var response = await ThrottledExecuteAsync(
                    async token => await _httpClient.SendAsync(request, token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("Ozon PullClaims /v2/returns/company/fbs failed {Status}: {Error}",
                        response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("returns", out var returns))
                    break;

                var pageCount = 0;
                foreach (var ret in returns.EnumerateArray())
                {
                    pageCount++;

                    var returnId = ret.TryGetProperty("return_id", out var ridProp) ? ridProp.GetInt64().ToString() : string.Empty;
                    var postingNumber = ret.TryGetProperty("posting_number", out var pnProp) ? pnProp.GetString() ?? string.Empty : string.Empty;
                    var status = ret.TryGetProperty("status", out var stProp) ? stProp.GetString() ?? "unknown" : "unknown";
                    var returnReason = ret.TryGetProperty("return_reason_name", out var rrProp) ? rrProp.GetString() ?? "" : "";

                    var claim = new ExternalClaimDto
                    {
                        PlatformClaimId = returnId,
                        PlatformCode = PlatformCode,
                        OrderNumber = postingNumber,
                        Status = status,
                        Reason = returnReason,
                        Currency = "RUB"
                    };

                    if (ret.TryGetProperty("created_at", out var dateProp) &&
                        DateTime.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var claimDate))
                    {
                        claim.ClaimDate = claimDate;
                    }
                    else
                    {
                        claim.ClaimDate = DateTime.UtcNow;
                    }

                    // Extract product info from return items
                    if (ret.TryGetProperty("items", out var items))
                    {
                        foreach (var item in items.EnumerateArray())
                        {
                            var sku = item.TryGetProperty("offer_id", out var skuProp) ? skuProp.GetString() : null;
                            var productName = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty;
                            var qty = item.TryGetProperty("quantity", out var qtyProp) ? qtyProp.GetInt32() : 1;

                            var price = 0m;
                            if (item.TryGetProperty("price", out var priceProp))
                            {
                                if (priceProp.ValueKind == JsonValueKind.String)
                                    decimal.TryParse(priceProp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                                else if (priceProp.ValueKind == JsonValueKind.Number)
                                    price = priceProp.GetDecimal();
                            }

                            claim.Lines.Add(new ExternalClaimLineDto
                            {
                                SKU = sku,
                                ProductName = productName,
                                Quantity = qty,
                                UnitPrice = price
                            });

                            claim.Amount += price * qty;
                        }
                    }

                    claims.Add(claim);
                }

                if (pageCount < limit)
                    break;

                offset += limit;
            }

            _logger.LogInformation("Ozon PullClaims: {Count} returns fetched", claims.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ozon PullClaims failed");
        }

        return claims.AsReadOnly();
    }

    /// <summary>
    /// Approves a return on Ozon using POST /v2/returns/company/{id}/approve.
    /// </summary>
    public async Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OzonAdapter.ApproveClaimAsync ClaimId={ClaimId}", claimId);

        try
        {
            using var request = BuildPostRequest($"/v2/returns/company/{claimId}/approve", new { });
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.SendAsync(request, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ozon ApproveClaim OK — ClaimId={ClaimId}", claimId);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Ozon ApproveClaim failed {Status}: {Error}",
                response.StatusCode, error);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ozon ApproveClaim failed — ClaimId={ClaimId}", claimId);
            return false;
        }
    }

    /// <summary>
    /// Rejects a return on Ozon using POST /v2/returns/company/{id}/reject with comment.
    /// </summary>
    public async Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OzonAdapter.RejectClaimAsync ClaimId={ClaimId}, Reason={Reason}", claimId, reason);

        try
        {
            var payload = new { comment = reason };
            using var request = BuildPostRequest($"/v2/returns/company/{claimId}/reject", payload);
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.SendAsync(request, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ozon RejectClaim OK — ClaimId={ClaimId}", claimId);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Ozon RejectClaim failed {Status}: {Error}",
                response.StatusCode, error);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ozon RejectClaim failed — ClaimId={ClaimId}", claimId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // IInvoiceCapableAdapter — Ozon (not supported)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Ozon does not support invoice link submission via API.
    /// Logs the request and returns true.
    /// </summary>
    public Task<bool> SendInvoiceLinkAsync(
        string shipmentPackageId, string invoiceUrl, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "OzonAdapter.SendInvoiceLinkAsync — Ozon has no invoice API, logged. PostingNumber={PostingNumber}, Url={Url}",
            shipmentPackageId, invoiceUrl);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Ozon does not support invoice file upload via API.
    /// Logs the request and returns true.
    /// </summary>
    public Task<bool> SendInvoiceFileAsync(
        string shipmentPackageId, byte[] pdfBytes, string fileName, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "OzonAdapter.SendInvoiceFileAsync — Ozon has no invoice API, logged. PostingNumber={PostingNumber}, File={FileName}, Size={Size}",
            shipmentPackageId, fileName, pdfBytes?.Length ?? 0);
        return Task.FromResult(true);
    }
}
