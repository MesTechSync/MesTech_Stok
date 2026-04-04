using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
/// Zalando Partner API adaptor — TAM implementasyon (D11-09).
/// Auth: OAuth2 Client Credentials Grant (ClientId:ClientSecret → Basic Auth → token endpoint).
/// Token URL: https://auth.zalando.com/oauth2/access_token
/// API Base:  https://api.zalando.com
/// Currency:  EUR (European marketplace)
/// Pagination: page-based with page + pageSize query params.
/// Implements IIntegratorAdapter + IOrderCapableAdapter.
/// </summary>
public sealed class ZalandoAdapter : IIntegratorAdapter, IOrderCapableAdapter, IPingableAdapter, IShipmentCapableAdapter, ISettlementCapableAdapter, IClaimCapableAdapter, IInvoiceCapableAdapter, IWebhookCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZalandoAdapter> _logger;
    private readonly ZalandoOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);
    private const int DefaultTokenExpirySeconds = 3600;

    // OAuth2 Client Credentials state
    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;
    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private bool _isConfigured;

    // 5-minute safety buffer before actual expiry
    private static readonly TimeSpan TokenBuffer = TimeSpan.FromMinutes(5);

    private readonly string TokenUrl;
    private readonly string ApiBase;

    public ZalandoAdapter(HttpClient httpClient, ILogger<ZalandoAdapter> logger,
        IOptions<ZalandoOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new ZalandoOptions();
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);
        TokenUrl = _options.TokenUrl;
        ApiBase = _options.ApiBaseUrl;

        // SSRF guard (G10853)
        if (Uri.TryCreate(ApiBase, UriKind.Absolute, out var uri) && Security.SsrfGuard.IsPrivateHost(uri.Host))
            _logger.LogWarning("[ZalandoAdapter] ApiBaseUrl points to private network: {Url}", ApiBase);

        // Seed from options if credentials provided
        if (!string.IsNullOrWhiteSpace(_options.ClientId))
        {
            _clientId = _options.ClientId;
            _clientSecret = _options.ClientSecret;
            _isConfigured = !string.IsNullOrWhiteSpace(_clientId) &&
                            !string.IsNullOrWhiteSpace(_clientSecret);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            // HTTP 429 rate-limit retry — Zalando Partner API throttling
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
                    _logger.LogWarning("[ZalandoAdapter] Rate limited (429). Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[ZalandoAdapter] API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[ZalandoAdapter] Circuit breaker OPENED for {Duration}s",
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

    public string PlatformCode => nameof(PlatformType.Zalando);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => false;

    // ═══════════════════════════════════════════
    // OAuth2 Client Credentials Token Management
    // ═══════════════════════════════════════════

    /// <summary>
    /// Cached OAuth2 Client Credentials Grant token with 5-minute buffer.
    /// Zalando uses Basic Auth (Base64 ClientId:ClientSecret) to obtain a Bearer token.
    /// </summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry - TokenBuffer)
            return _accessToken;

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
            cancellationToken: ct).ConfigureAwait(false);

        _accessToken = json.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
        var expiresIn = json.RootElement.TryGetProperty("expires_in", out var expEl)
            ? expEl.GetInt32()
            : DefaultTokenExpirySeconds;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

        _logger.LogInformation("Zalando OAuth2 token refreshed — expires in {Seconds}s", expiresIn);
        return _accessToken;
    }

    private void ConfigureCredentials(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _clientId = credentials.GetValueOrDefault("ClientId", string.Empty);
        _clientSecret = credentials.GetValueOrDefault("ClientSecret", string.Empty);
        _isConfigured = !string.IsNullOrWhiteSpace(_clientId) &&
                        !string.IsNullOrWhiteSpace(_clientSecret);
        // Invalidate cached token when credentials change
        _accessToken = string.Empty;
        _tokenExpiry = DateTime.MinValue;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "ZalandoAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(_accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return request;
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
                result.ErrorMessage = "Zalando: ClientId veya ClientSecret eksik";
                return result;
            }

            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            // Probe: fetch a single article to confirm API access
            var url = $"{ApiBase}/partner/articles?page=0&pageSize=1";
            using var response = await ThrottledExecuteAsync(
                async token => await _httpClient.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, url), token).ConfigureAwait(false), ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;
            result.IsSuccess = response.IsSuccessStatusCode;

            if (result.IsSuccess)
            {
                result.StoreName = "Zalando Partner (OAuth2 OK)";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zalando TestConnectionAsync basarisiz");
            result.ErrorMessage = ex.Message;
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

    /// <summary>
    /// Pulls articles (products) from Zalando Partner API with page-based pagination.
    /// GET /partner/articles?page={page}&amp;pageSize={pageSize}
    /// </summary>
    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ZalandoAdapter.PullProductsAsync called");

        var products = new List<Product>();

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            const int pageSize = 50;
            var page = 0;
            var hasMore = true;

            while (hasMore)
            {
                var url = $"{ApiBase}/partner/articles?page={page}&pageSize={pageSize}";
                using var response = await ThrottledExecuteAsync(
                    async token => await _httpClient.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, url), token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("Zalando PullProducts failed: {Status} - {Error}", response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                // Zalando Partner API response: { "content": [...], "totalElements": N }
                if (!doc.RootElement.TryGetProperty("content", out var items))
                    break;

                var pageCount = 0;
                foreach (var item in items.EnumerateArray())
                {
                    pageCount++;

                    var sku = item.TryGetProperty("sku", out var skuEl) ? skuEl.GetString() ?? "" : "";
                    var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
                    var stock = 0;
                    var price = 0m;

                    // Zalando returns availableUnits per article
                    if (item.TryGetProperty("availableUnits", out var unitsEl))
                        stock = unitsEl.GetInt32();

                    // Price in EUR — Zalando uses "price.amount" structure
                    if (item.TryGetProperty("price", out var priceEl) &&
                        priceEl.TryGetProperty("amount", out var amountEl))
                    {
                        decimal.TryParse(amountEl.GetString(), NumberStyles.Number,
                            CultureInfo.InvariantCulture, out price);
                    }

                    products.Add(new Product
                    {
                        Name = name,
                        SKU = sku,
                        Stock = stock,
                        SalePrice = price
                    });
                }

                page++;
                var totalElements = doc.RootElement.TryGetProperty("totalElements", out var totalEl)
                    ? totalEl.GetInt32()
                    : 0;
                hasMore = pageCount == pageSize && (page * pageSize) < totalElements;
            }

            _logger.LogInformation("Zalando PullProducts: {Count} articles retrieved", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zalando PullProducts failed");
        }

        return products.AsReadOnly();
    }

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        // Zalando Partner API does not support product creation via REST —
        // new articles must be submitted through the Merchant Office portal.
        _logger.LogWarning(
            "ZalandoAdapter.PushProductAsync — Zalando does not support article creation via REST API. " +
            "Use Merchant Office portal for new product onboarding. SKU={SKU}",
            product.SKU);
        return Task.FromResult(false);
    }

    /// <summary>
    /// Updates stock (inventory) for a product on Zalando Partner API.
    /// POST /partner/inventory
    /// Body: { "sku": "...", "availableUnits": N }
    /// </summary>
    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ZalandoAdapter.PushStockUpdateAsync: ProductId={ProductId} qty={Qty}",
            productId, newStock);

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                sku = productId.ToString(),
                availableUnits = newStock
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post, $"{ApiBase}/partner/inventory");
                    req.Content = content;
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Zalando StockUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Zalando StockUpdate success: SKU={SKU} qty={Qty}", productId, newStock);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zalando StockUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Updates price for a product on Zalando Partner API.
    /// POST /partner/prices
    /// Body: { "sku": "...", "price": { "amount": "9.99", "currency": "EUR" } }
    /// Note: Zalando operates exclusively in EUR.
    /// </summary>
    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ZalandoAdapter.PushPriceUpdateAsync: ProductId={ProductId} price={Price} EUR",
            productId, newPrice);

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                sku = productId.ToString(),
                price = new
                {
                    amount = newPrice.ToString("F2", CultureInfo.InvariantCulture),
                    currency = "EUR"
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post, $"{ApiBase}/partner/prices");
                    req.Content = content;
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Zalando PriceUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Zalando PriceUpdate success: SKU={SKU} price={Price} EUR",
                productId, newPrice);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zalando PriceUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Returns empty list — Zalando manages its own category taxonomy and does not
    /// expose a category listing endpoint in the Partner API.
    /// </summary>
    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("ZalandoAdapter.GetCategoriesAsync — Zalando manages categories internally");
        return Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());
    }

    // ═══════════════════════════════════════════
    // IOrderCapableAdapter — Orders
    // ═══════════════════════════════════════════

    /// <summary>
    /// Pulls orders from Zalando Partner API.
    /// GET /partner/orders?createdAfter={date}
    /// </summary>
    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ZalandoAdapter.PullOrdersAsync since={Since}", since);

        var orders = new List<ExternalOrderDto>();

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var sinceDate = since ?? DateTime.UtcNow.AddDays(-30);
            var sinceStr = sinceDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

            const int pageSize = 50;
            var page = 0;
            var hasMore = true;

            while (hasMore)
            {
                var url = $"{ApiBase}/partner/orders?createdAfter={Uri.EscapeDataString(sinceStr)}" +
                          $"&page={page}&pageSize={pageSize}";
                using var response = await ThrottledExecuteAsync(
                    async token => await _httpClient.SendAsync(CreateAuthenticatedRequest(HttpMethod.Get, url), token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("Zalando PullOrders failed: {Status} - {Error}", response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("content", out var ordersArr))
                    break;

                var pageCount = 0;
                foreach (var orderEl in ordersArr.EnumerateArray())
                {
                    pageCount++;

                    var orderId = orderEl.TryGetProperty("orderId", out var oidEl)
                        ? oidEl.GetString() ?? ""
                        : "";
                    var orderNumber = orderEl.TryGetProperty("orderNumber", out var onEl)
                        ? onEl.GetString() ?? orderId
                        : orderId;
                    var status = orderEl.TryGetProperty("status", out var stEl)
                        ? stEl.GetString() ?? ""
                        : "";

                    var orderDate = DateTime.UtcNow;
                    if (orderEl.TryGetProperty("createdAt", out var createdEl) &&
                        DateTime.TryParse(createdEl.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var parsedDate))
                    {
                        orderDate = parsedDate;
                    }

                    DateTime? lastModified = null;
                    if (orderEl.TryGetProperty("modifiedAt", out var modEl) &&
                        DateTime.TryParse(modEl.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var parsedMod))
                    {
                        lastModified = parsedMod;
                    }

                    var order = new ExternalOrderDto
                    {
                        PlatformCode = PlatformCode,
                        PlatformOrderId = orderId,
                        OrderNumber = orderNumber,
                        Status = status,
                        OrderDate = orderDate,
                        LastModifiedDate = lastModified,
                        Currency = "EUR" // Zalando operates exclusively in EUR
                    };

                    // Total amount
                    if (orderEl.TryGetProperty("totalAmount", out var totalEl))
                    {
                        if (totalEl.TryGetProperty("amount", out var amountEl))
                        {
                            decimal.TryParse(amountEl.GetString(), NumberStyles.Number,
                                CultureInfo.InvariantCulture, out var totalAmount);
                            order.TotalAmount = totalAmount;
                        }
                    }

                    // Shipping cost
                    if (orderEl.TryGetProperty("shippingCost", out var shippingEl) &&
                        shippingEl.TryGetProperty("amount", out var shAmountEl))
                    {
                        decimal.TryParse(shAmountEl.GetString(), NumberStyles.Number,
                            CultureInfo.InvariantCulture, out var shippingCost);
                        order.ShippingCost = shippingCost;
                    }

                    // Customer info
                    if (orderEl.TryGetProperty("customer", out var customer))
                    {
                        var firstName = customer.TryGetProperty("firstName", out var fnEl)
                            ? fnEl.GetString() ?? ""
                            : "";
                        var lastName = customer.TryGetProperty("lastName", out var lnEl)
                            ? lnEl.GetString() ?? ""
                            : "";
                        order.CustomerName = $"{firstName} {lastName}".Trim();
                        order.CustomerEmail = customer.TryGetProperty("email", out var emailEl)
                            ? emailEl.GetString()
                            : null;
                    }

                    // Delivery address
                    if (orderEl.TryGetProperty("deliveryAddress", out var addr))
                    {
                        var street = addr.TryGetProperty("addressLine1", out var al1El)
                            ? al1El.GetString() ?? ""
                            : "";
                        var city = addr.TryGetProperty("city", out var cityEl)
                            ? cityEl.GetString() ?? ""
                            : "";
                        order.CustomerAddress = $"{street}, {city}".Trim(' ', ',');
                        order.CustomerCity = string.IsNullOrEmpty(city) ? null : city;

                        if (string.IsNullOrEmpty(order.CustomerName) &&
                            addr.TryGetProperty("name", out var addrNameEl))
                        {
                            order.CustomerName = addrNameEl.GetString() ?? "";
                        }
                    }

                    // Order lines
                    if (orderEl.TryGetProperty("orderItems", out var lineItems))
                    {
                        foreach (var lineEl in lineItems.EnumerateArray())
                        {
                            var lineId = lineEl.TryGetProperty("lineItemId", out var liEl)
                                ? liEl.GetString()
                                : null;
                            var sku = lineEl.TryGetProperty("sku", out var skuEl)
                                ? skuEl.GetString()
                                : null;
                            var productName = lineEl.TryGetProperty("name", out var pnEl)
                                ? pnEl.GetString() ?? ""
                                : "";
                            var qty = lineEl.TryGetProperty("quantity", out var qtyEl)
                                ? qtyEl.GetInt32()
                                : 1;

                            var unitPrice = 0m;
                            if (lineEl.TryGetProperty("unitPrice", out var upEl) &&
                                upEl.TryGetProperty("amount", out var upAmountEl))
                            {
                                decimal.TryParse(upAmountEl.GetString(), NumberStyles.Number,
                                    CultureInfo.InvariantCulture, out unitPrice);
                            }

                            order.Lines.Add(new ExternalOrderLineDto
                            {
                                PlatformLineId = lineId,
                                SKU = sku,
                                ProductName = productName,
                                Quantity = qty,
                                UnitPrice = unitPrice,
                                TaxRate = 0m, // Zalando does not expose per-line tax rate
                                LineTotal = unitPrice * qty
                            });
                        }
                    }

                    orders.Add(order);
                }

                page++;
                var totalElements = doc.RootElement.TryGetProperty("totalElements", out var totalCountEl)
                    ? totalCountEl.GetInt32()
                    : 0;
                hasMore = pageCount == pageSize && (page * pageSize) < totalElements;
            }

            _logger.LogInformation("Zalando PullOrders: {Count} orders retrieved", orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zalando PullOrders failed");
        }

        return orders.AsReadOnly();
    }

    /// <summary>
    /// Updates order status on Zalando Partner API.
    /// PUT /partner/orders/{orderId}/status
    /// </summary>
    public async Task<bool> UpdateOrderStatusAsync(string packageId, string status,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ZalandoAdapter.UpdateOrderStatusAsync: OrderId={OrderId} Status={Status}",
            packageId, status);

        if (string.IsNullOrWhiteSpace(packageId))
        {
            _logger.LogWarning("ZalandoAdapter.UpdateOrderStatusAsync — packageId is required");
            return false;
        }

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var encodedId = Uri.EscapeDataString(packageId);
            var url = $"{ApiBase}/partner/orders/{encodedId}/status";

            var payload = new { status };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Put, url);
                    req.Content = content;
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Zalando UpdateOrderStatus failed: {Status} - {Error}",
                    response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Zalando UpdateOrderStatus success: OrderId={OrderId} Status={Status}",
                packageId, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zalando UpdateOrderStatus exception: {OrderId}", packageId);
            return false;
        }
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
                new Uri(ApiBase, UriKind.Absolute));
            using var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);

            _logger.LogDebug("Zalando ping: {StatusCode}", response.StatusCode);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Zalando ping failed");
            return false;
        }
    }

    // ── IShipmentCapableAdapter ──
    public async Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber, MesTech.Domain.Enums.CargoProvider provider, CancellationToken ct = default)
    {
        _logger.LogInformation("[ZalandoAdapter] SendShipment — Order:{Order}", platformOrderId);

        try
        {
            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);
            var payload = JsonSerializer.Serialize(new { tracking_number = trackingNumber, carrier = provider.ToString() });

            using var response = await ThrottledExecuteAsync(async c =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/merchants/orders/{platformOrderId}/shipments")
                { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return await _httpClient.SendAsync(req, c).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ZalandoAdapter] SendShipment error");
            return false;
        }
    }

    // ── ISettlementCapableAdapter ──
    public Task<SettlementDto?> GetSettlementAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        _logger.LogWarning("[ZalandoAdapter] GetSettlement — Zalando Settlements API not available for all partners");
        return Task.FromResult<SettlementDto?>(null);
    }

    public Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(DateTime startDate, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CargoInvoiceDto>>(Array.Empty<CargoInvoiceDto>());

    // ── IClaimCapableAdapter ──
    public async Task<IReadOnlyList<ExternalClaimDto>> PullClaimsAsync(DateTime? since = null, CancellationToken ct = default)
    {
        _logger.LogInformation("[ZalandoAdapter] PullClaims");

        try
        {
            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);

            using var response = await ThrottledExecuteAsync(async c =>
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/merchants/returns?status=APPROVED");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return await _httpClient.SendAsync(req, c).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return Array.Empty<ExternalClaimDto>();

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(body);
            var claims = new List<ExternalClaimDto>();

            if (doc.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var r in items.EnumerateArray())
                {
                    var rid = r.TryGetProperty("return_id", out var id) ? id.GetString() ?? "" : "";
                    claims.Add(new ExternalClaimDto { PlatformClaimId = rid, PlatformCode = "Zalando", Status = "APPROVED" });
                }
            }

            return claims;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ZalandoAdapter] PullClaims error");
            return Array.Empty<ExternalClaimDto>();
        }
    }
    public Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    { _logger.LogDebug("[ZalandoAdapter] RejectClaim — Zalando auto-accepts returns"); return Task.FromResult(false); }

    // ── IInvoiceCapableAdapter ──
    public Task<bool> SendInvoiceLinkAsync(string shipmentPackageId, string invoiceUrl, CancellationToken ct = default)
    { _logger.LogDebug("[ZalandoAdapter] SendInvoiceLink not supported — Zalando generates invoices"); return Task.FromResult(false); }
    public Task<bool> SendInvoiceFileAsync(string shipmentPackageId, byte[] pdfBytes, string fileName, CancellationToken ct = default)
    { _logger.LogDebug("[ZalandoAdapter] SendInvoiceFile not supported — Zalando generates invoices"); return Task.FromResult(false); }

    // ── IWebhookCapableAdapter ──
    public async Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ZalandoAdapter.RegisterWebhookAsync: {Url}", callbackUrl);

        try
        {
            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var payload = JsonSerializer.Serialize(new
            {
                callback_url = callbackUrl,
                event_types = new[] { "ORDER_CREATED", "ORDER_UPDATED", "RETURN_CREATED" }
            }, _jsonOptions);

            using var response = await ThrottledExecuteAsync(
                async c =>
                {
                    using var req = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/merchants/webhooks")
                    { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    return await _httpClient.SendAsync(req, c).ConfigureAwait(false);
                },
                ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Zalando RegisterWebhook failed: {Status} {Error}", response.StatusCode, error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zalando RegisterWebhook exception");
            return false;
        }
    }

    public async Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("ZalandoAdapter.UnregisterWebhookAsync");

        try
        {
            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);

            using var response = await ThrottledExecuteAsync(
                async c =>
                {
                    using var req = new HttpRequestMessage(HttpMethod.Delete, $"{ApiBase}/merchants/webhooks");
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    return await _httpClient.SendAsync(req, c).ConfigureAwait(false);
                },
                ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Zalando UnregisterWebhook failed: {Status} {Error}", response.StatusCode, error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zalando UnregisterWebhook exception");
            return false;
        }
    }

    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var eventType = doc.RootElement.TryGetProperty("event_type", out var et) ? et.GetString() : "unknown";
            _logger.LogInformation("ZalandoAdapter webhook processed: EventType={EventType} PayloadLength={Length}", eventType, payload.Length);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[Zalando] Malformed webhook payload ({Length}b)", payload?.Length ?? 0);
        }
        return Task.CompletedTask;
    }
}
