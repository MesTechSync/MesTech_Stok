using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// PTT AVM platform adaptoru — H31 real HTTP implementation.
/// Username + Password -> Bearer token exchange (cached, 5-min buffer).
/// Implements IIntegratorAdapter + IOrderCapableAdapter.
/// Orders: GET /api/orders
/// Stock: PUT /api/product/stock
/// Price: PUT /api/product/price
/// </summary>
public sealed class PttAvmAdapter : IIntegratorAdapter, IOrderCapableAdapter, IPingableAdapter,
    IShipmentCapableAdapter, ISettlementCapableAdapter, IClaimCapableAdapter, IInvoiceCapableAdapter, IWebhookCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PttAvmAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);

    // Username/Password -> Bearer token exchange
    private string _username = Environment.GetEnvironmentVariable("PTTAVM_USERNAME") ?? string.Empty;
    private string _password = Environment.GetEnvironmentVariable("PTTAVM_PASSWORD") ?? string.Empty;
    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private string _baseUrl;
    private string _tokenEndpoint;
    private bool _isConfigured;

    // 5-minute safety buffer before actual expiry
    private static readonly TimeSpan TokenBuffer = TimeSpan.FromMinutes(5);

    public PttAvmAdapter(HttpClient httpClient, ILogger<PttAvmAdapter> logger,
        IOptions<PttAvmOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var opts = options?.Value ?? new PttAvmOptions();
        _httpClient.Timeout = TimeSpan.FromSeconds(opts.HttpTimeoutSeconds);
        _baseUrl = opts.BaseUrl;
        _tokenEndpoint = opts.TokenEndpoint;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            // HTTP 429 rate-limit retry — PttAVM API throttling
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
                    _logger.LogWarning("[PttAvmAdapter] Rate limited (429). Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[PttAvmAdapter] API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[PttAvmAdapter] Circuit breaker OPENED for {Duration}s",
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

    public string PlatformCode => nameof(PlatformType.PttAVM);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ═══════════════════════════════════════════
    // Username/Password Bearer Token Exchange
    // ═══════════════════════════════════════════

    /// <summary>
    /// Exchanges username/password for a Bearer token (cached with 5-min buffer).
    /// </summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry - TokenBuffer)
            return _accessToken;

        var loginPayload = JsonSerializer.Serialize(new
        {
            username = _username,
            password = _password
        }, _jsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
        request.Content = new StringContent(loginPayload, Encoding.UTF8, "application/json");

        using var response = await ThrottledExecuteAsync(async cancellationToken => 
            await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false), ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
            cancellationToken: ct).ConfigureAwait(false);

        _accessToken = json.RootElement.GetProperty("token").GetString() ?? string.Empty;

        // PTT AVM tokens expire in 1 hour; parse if available, else default
        if (json.RootElement.TryGetProperty("expiresIn", out var expiresInEl))
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresInEl.GetInt32());
        else
            _tokenExpiry = DateTime.UtcNow.AddHours(1);

        _logger.LogInformation("PttAVM Bearer token refreshed — expires at {Expiry:u}", _tokenExpiry);
        return _accessToken;
    }

    private void ConfigureCredentials(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _username = credentials.GetValueOrDefault("Username", string.Empty);
        _password = credentials.GetValueOrDefault("Password", string.Empty);

        var rawPttBaseUrl = credentials.GetValueOrDefault("BaseUrl", "");
        if (!string.IsNullOrEmpty(rawPttBaseUrl))
        {
            if (!Uri.TryCreate(rawPttBaseUrl, UriKind.Absolute, out var parsedUri) ||
                (parsedUri.Scheme != "https" && parsedUri.Scheme != "http"))
                throw new ArgumentException($"Invalid PttAvm base URL scheme: {rawPttBaseUrl}. Only HTTP(S) allowed.");
            if (SsrfGuard.IsPrivateHost(parsedUri.Host))
                _logger.LogWarning("[PttAvmAdapter] BaseUrl points to private network: {BaseUrl}", rawPttBaseUrl);
            _baseUrl = rawPttBaseUrl;
        }
        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("TokenEndpoint")))
            _tokenEndpoint = credentials["TokenEndpoint"];

        _isConfigured = !string.IsNullOrWhiteSpace(_username) &&
                        !string.IsNullOrWhiteSpace(_password);
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "PttAvmAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
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
                result.ErrorMessage = "PttAVM: Username veya Password eksik";
                return result;
            }

            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);
            result.IsSuccess = !string.IsNullOrEmpty(token);
            result.StoreName = "PTT AVM Satici";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PttAVM TestConnectionAsync basarisiz");
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
    /// Creates a product listing on PTT AVM.
    /// POST /api/product/create with JSON payload.
    /// Returns true on 2xx, false otherwise.
    /// </summary>
    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PttAvmAdapter.PushProductAsync: SKU={SKU} Name={Name}",
            product.SKU, product.Name);

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                barcode = product.SKU ?? string.Empty,
                productName = product.Name ?? string.Empty,
                stockQuantity = product.Stock,
                salePrice = product.SalePrice,
                description = product.Description ?? product.Name ?? string.Empty,
                currencyType = "TRY"
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}/api/product/create";
            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post, url);
                    req.Content = content;
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("PttAVM PushProduct failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("PttAVM PushProduct success: SKU={SKU}", product.SKU);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM PushProduct exception: SKU={SKU}", product.SKU);
            return false;
        }
    }

    /// <summary>
    /// Pulls products from PTT AVM.
    /// GET /api/product/list?page={page}&amp;size={size}
    /// </summary>
    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PttAvmAdapter.PullProductsAsync called");

        var products = new List<Product>();

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            const int pageSize = 100;
            var page = 1;
            var hasMore = true;

            while (hasMore)
            {
                var url = $"{_baseUrl}/api/product/list?page={page}&size={pageSize}";
                using var response = await ThrottledExecuteAsync(
                    async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get, url);
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("PttAVM PullProducts failed: {Status} - {Error}", response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("data", out var items))
                    break;

                var pageCount = 0;
                foreach (var item in items.EnumerateArray())
                {
                    pageCount++;
                    var name = item.TryGetProperty("productName", out var nameEl)
                        ? nameEl.GetString() ?? string.Empty
                        : string.Empty;

                    var sku = item.TryGetProperty("barcode", out var skuEl)
                        ? skuEl.GetString() ?? string.Empty
                        : string.Empty;

                    var stock = item.TryGetProperty("stockQuantity", out var stockEl)
                        ? stockEl.GetInt32()
                        : 0;

                    var price = 0m;
                    if (item.TryGetProperty("salePrice", out var priceEl))
                    {
                        decimal.TryParse(priceEl.GetRawText(),
                            NumberStyles.Any, CultureInfo.InvariantCulture, out price);
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
                hasMore = pageCount == pageSize;
            }

            _logger.LogInformation("PttAVM PullProducts: {Count} products retrieved", products.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM PullProducts failed");
        }

        return products.AsReadOnly();
    }

    /// <summary>
    /// Updates stock quantity for a product on PTT AVM.
    /// PUT /api/product/stock with JSON payload { productId, stockQuantity }.
    /// Returns true on 2xx, false otherwise.
    /// </summary>
    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PttAvmAdapter.PushStockUpdateAsync: ProductId={ProductId} qty={Qty}",
            productId, newStock);

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                productId = productId.ToString(),
                stockQuantity = newStock
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}/api/product/stock";
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
                _logger.LogError("PttAVM StockUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("PttAVM StockUpdate success: ProductId={ProductId} qty={Qty}",
                productId, newStock);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM StockUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Updates the price of a product on PTT AVM.
    /// PUT /api/product/price with JSON payload { productId, price }.
    /// Returns true on 2xx, false otherwise.
    /// </summary>
    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PttAvmAdapter.PushPriceUpdateAsync: ProductId={ProductId} price={Price}",
            productId, newPrice);

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                productId = productId.ToString(),
                price = newPrice
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}/api/product/price";
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
                _logger.LogError("PttAVM PriceUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("PttAVM PriceUpdate success: ProductId={ProductId} price={Price}",
                productId, newPrice);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM PriceUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Pulls category tree from PTT AVM.
    /// GET /api/category/list
    /// Response: { "data": [ { categoryId, categoryName, parentId, subCategories: [...] } ] }
    /// </summary>
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PttAvmAdapter.GetCategoriesAsync called");

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var url = $"{_baseUrl}/api/category/list";
            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get, url);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("PttAVM GetCategories failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<CategoryDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("data", out var dataArr))
                return Array.Empty<CategoryDto>();

            var categories = new List<CategoryDto>();
            foreach (var item in dataArr.EnumerateArray())
            {
                categories.Add(ParseCategory(item, parentId: null));
            }

            _logger.LogInformation("PttAVM GetCategories: {Count} top-level categories", categories.Count);
            return categories.AsReadOnly();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM GetCategories failed");
            return Array.Empty<CategoryDto>();
        }
    }

    /// <summary>
    /// Recursively parses a category JSON element into a CategoryDto.
    /// </summary>
    private static CategoryDto ParseCategory(JsonElement element, int? parentId)
    {
        var categoryId = element.TryGetProperty("categoryId", out var idEl)
            ? idEl.GetInt32()
            : 0;

        var name = element.TryGetProperty("categoryName", out var nameEl)
            ? nameEl.GetString() ?? string.Empty
            : string.Empty;

        var dto = new CategoryDto
        {
            PlatformCategoryId = categoryId,
            Name = name,
            ParentId = parentId
        };

        if (element.TryGetProperty("subCategories", out var subArr))
        {
            foreach (var sub in subArr.EnumerateArray())
            {
                dto.SubCategories.Add(ParseCategory(sub, parentId: categoryId));
            }
        }

        return dto;
    }

    // ═══════════════════════════════════════════
    // IOrderCapableAdapter — Orders
    // ═══════════════════════════════════════════

    /// <summary>
    /// Pulls orders from PTT AVM.
    /// GET /api/orders?startDate={since}&amp;page={page}&amp;size={size}
    /// Response: { "data": [ { orderId, status, totalAmount, orderDate, ... } ] }
    /// </summary>
    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PttAvmAdapter.PullOrdersAsync since={Since}", since);

        var orders = new List<ExternalOrderDto>();

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var sinceDate = since ?? DateTime.UtcNow.AddDays(-30);
            var sinceStr = sinceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            const int pageSize = 50;
            var page = 1;
            var hasMore = true;

            while (hasMore)
            {
                var url = $"{_baseUrl}/api/orders?startDate={sinceStr}&page={page}&size={pageSize}";
                using var response = await ThrottledExecuteAsync(
                    async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get, url);
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("PttAVM PullOrders failed: {Status} - {Error}", response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("data", out var dataArr))
                    break;

                var pageCount = 0;
                foreach (var orderEl in dataArr.EnumerateArray())
                {
                    pageCount++;

                    var orderId = orderEl.TryGetProperty("orderId", out var oidEl)
                        ? oidEl.GetString() ?? string.Empty
                        : string.Empty;

                    var status = orderEl.TryGetProperty("status", out var stEl)
                        ? stEl.GetString() ?? string.Empty
                        : string.Empty;

                    var orderDate = DateTime.UtcNow;
                    if (orderEl.TryGetProperty("orderDate", out var dateEl) &&
                        DateTime.TryParse(dateEl.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var parsedDate))
                    {
                        orderDate = parsedDate;
                    }

                    var totalAmount = 0m;
                    if (orderEl.TryGetProperty("totalAmount", out var totalEl))
                    {
                        decimal.TryParse(totalEl.GetRawText(),
                            NumberStyles.Any, CultureInfo.InvariantCulture, out totalAmount);
                    }

                    var order = new ExternalOrderDto
                    {
                        PlatformCode = PlatformCode,
                        PlatformOrderId = orderId,
                        OrderNumber = orderId,
                        Status = status,
                        OrderDate = orderDate,
                        TotalAmount = totalAmount,
                        Currency = "TRY"
                    };

                    // Customer info
                    if (orderEl.TryGetProperty("customerName", out var cnEl))
                        order.CustomerName = cnEl.GetString() ?? string.Empty;
                    if (orderEl.TryGetProperty("customerPhone", out var cpEl))
                        order.CustomerPhone = cpEl.GetString();
                    if (orderEl.TryGetProperty("customerAddress", out var caEl))
                        order.CustomerAddress = caEl.GetString();
                    if (orderEl.TryGetProperty("customerCity", out var ccEl))
                        order.CustomerCity = ccEl.GetString();

                    // Cargo info
                    if (orderEl.TryGetProperty("cargoTrackingNumber", out var ctnEl))
                        order.CargoTrackingNumber = ctnEl.GetString();

                    // Line items
                    if (orderEl.TryGetProperty("lines", out var linesArr))
                    {
                        foreach (var lineEl in linesArr.EnumerateArray())
                        {
                            var sku = lineEl.TryGetProperty("sku", out var skuEl)
                                ? skuEl.GetString()
                                : null;
                            var productName = lineEl.TryGetProperty("productName", out var pnEl)
                                ? pnEl.GetString() ?? string.Empty
                                : string.Empty;
                            var qty = lineEl.TryGetProperty("quantity", out var qtyEl)
                                ? qtyEl.GetInt32()
                                : 1;
                            var unitPrice = 0m;
                            if (lineEl.TryGetProperty("unitPrice", out var upEl))
                            {
                                decimal.TryParse(upEl.GetRawText(),
                                    NumberStyles.Any, CultureInfo.InvariantCulture, out unitPrice);
                            }

                            order.Lines.Add(new ExternalOrderLineDto
                            {
                                SKU = sku,
                                ProductName = productName,
                                Quantity = qty,
                                UnitPrice = unitPrice,
                                LineTotal = unitPrice * qty
                            });
                        }
                    }

                    orders.Add(order);
                }

                page++;
                hasMore = pageCount == pageSize;
            }

            _logger.LogInformation("PttAVM PullOrders: {Count} orders retrieved", orders.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM PullOrders failed");
        }

        return orders.AsReadOnly();
    }

    /// <summary>
    /// PTT AVM does not support arbitrary order status updates via API.
    /// Returns false.
    /// </summary>
    public Task<bool> UpdateOrderStatusAsync(
        string packageId, string status, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "PttAvmAdapter.UpdateOrderStatusAsync — not supported. Package={PackageId} Status={Status}",
            packageId, status);
        return Task.FromResult(false);
    }

    // ═══════════════════════════════════════════
    // IShipmentCapableAdapter — Shipment Notification
    // ═══════════════════════════════════════════

    /// <summary>
    /// Sends shipment notification to PTT AVM.
    /// POST /api/order/shipment with orderId, trackingNumber, cargoCompany.
    /// </summary>
    public async Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber,
        CargoProvider provider, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            if (string.IsNullOrWhiteSpace(platformOrderId))
            {
                _logger.LogWarning("PttAVM SendShipment — platformOrderId bos olamaz");
                return false;
            }

            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                _logger.LogWarning("PttAVM SendShipment — trackingNumber bos olamaz. OrderId={OrderId}",
                    platformOrderId);
                return false;
            }

            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var cargoCompany = MapCargoProviderToPttAvm(provider);

            var payload = new
            {
                orderId = platformOrderId,
                trackingNumber,
                cargoCompany
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}/api/order/shipment";

            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post, url);
                    req.Content = content;
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "PttAVM SendShipment basarili — OrderId={OrderId}, Tracking={Tracking}, Cargo={Cargo}",
                    platformOrderId, trackingNumber, cargoCompany);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("PttAVM SendShipment basarisiz {Status}: {Error}",
                response.StatusCode, error);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM SendShipment hatasi — OrderId={OrderId}", platformOrderId);
            return false;
        }
    }

    /// <summary>
    /// Maps CargoProvider enum to PTT AVM cargo company name.
    /// </summary>
    private static string MapCargoProviderToPttAvm(CargoProvider provider) => provider switch
    {
        CargoProvider.PttKargo => "PTT Kargo",
        CargoProvider.YurticiKargo => "Yurtiçi Kargo",
        CargoProvider.ArasKargo => "Aras Kargo",
        CargoProvider.SuratKargo => "Sürat Kargo",
        CargoProvider.MngKargo => "MNG Kargo",
        CargoProvider.Hepsijet => "Hepsijet",
        CargoProvider.UPS => "UPS",
        CargoProvider.Sendeo => "Sendeo",
        CargoProvider.DHL => "DHL",
        CargoProvider.FedEx => "FedEx",
        _ => provider.ToString()
    };

    // ═══════════════════════════════════════════
    // ISettlementCapableAdapter — Cari Hesap
    // ═══════════════════════════════════════════

    /// <summary>
    /// Pulls settlement data from PTT AVM.
    /// GET /api/settlement?startDate={start}&amp;endDate={end}
    /// </summary>
    public async Task<SettlementDto?> GetSettlementAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PttAvmAdapter.GetSettlementAsync: {StartDate} — {EndDate}", startDate, endDate);

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var settlement = new SettlementDto
            {
                PlatformCode = "PttAVM",
                StartDate = startDate,
                EndDate = endDate,
                Currency = "TRY"
            };

            var startStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var endStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var url = $"{_baseUrl}/api/settlement?startDate={startStr}&endDate={endStr}";

            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get, url);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("PttAVM GetSettlement failed: {Status} - {Error}", response.StatusCode, error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataArr.EnumerateArray())
                {
                    var orderNumber = item.TryGetProperty("orderNumber", out var onEl)
                        ? onEl.GetString() : null;

                    var txType = item.TryGetProperty("transactionType", out var ttEl)
                        ? ttEl.GetString() ?? "SALE" : "SALE";

                    var amount = 0m;
                    if (item.TryGetProperty("amount", out var amtEl))
                        decimal.TryParse(amtEl.GetRawText(), NumberStyles.Any, CultureInfo.InvariantCulture, out amount);

                    decimal? commission = null;
                    if (item.TryGetProperty("commission", out var comEl))
                    {
                        if (decimal.TryParse(comEl.GetRawText(), NumberStyles.Any, CultureInfo.InvariantCulture, out var comVal))
                            commission = comVal;
                    }

                    var txDate = DateTime.UtcNow;
                    if (item.TryGetProperty("transactionDate", out var tdEl) &&
                        DateTime.TryParse(tdEl.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var parsedTxDate))
                    {
                        txDate = parsedTxDate;
                    }

                    settlement.Lines.Add(new SettlementLineDto
                    {
                        OrderNumber = orderNumber,
                        TransactionType = txType,
                        Amount = amount,
                        CommissionAmount = commission,
                        TransactionDate = txDate
                    });

                    if (txType == "SALE")
                    {
                        settlement.TotalSales += amount;
                        settlement.TotalCommission += commission ?? 0m;
                    }
                    else if (txType == "RETURN")
                    {
                        settlement.TotalReturnDeduction += amount;
                    }
                }
            }

            settlement.NetAmount = settlement.TotalSales - settlement.TotalCommission - settlement.TotalReturnDeduction;

            _logger.LogInformation("PttAVM GetSettlement: {LineCount} lines, Net={Net} TRY",
                settlement.Lines.Count, settlement.NetAmount);
            return settlement;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM GetSettlement exception: {StartDate}—{EndDate}", startDate, endDate);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(DateTime startDate, CancellationToken ct = default)
    {
        _logger.LogInformation("PttAvmAdapter.GetCargoInvoicesAsync: PttAVM does not expose cargo invoices — returning empty list. StartDate={StartDate}", startDate);
        return Task.FromResult<IReadOnlyList<CargoInvoiceDto>>(Array.Empty<CargoInvoiceDto>());
    }

    // ═══════════════════════════════════════════
    // IClaimCapableAdapter — Iade Yonetimi
    // ═══════════════════════════════════════════

    /// <summary>
    /// Pulls return/claim requests from PTT AVM.
    /// GET /api/return-requests?startDate={since}
    /// </summary>
    public async Task<IReadOnlyList<ExternalClaimDto>> PullClaimsAsync(DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PttAvmAdapter.PullClaimsAsync since={Since}", since);

        var claims = new List<ExternalClaimDto>();

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var sinceDate = since ?? DateTime.UtcNow.AddDays(-30);
            var sinceStr = sinceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var url = $"{_baseUrl}/api/return-requests?startDate={sinceStr}";

            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get, url);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("PttAVM PullClaims failed: {Status} - {Error}", response.StatusCode, error);
                return claims.AsReadOnly();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("data", out var dataArr) || dataArr.ValueKind != JsonValueKind.Array)
                return claims.AsReadOnly();

            foreach (var item in dataArr.EnumerateArray())
            {
                var claimId = item.TryGetProperty("returnRequestId", out var crIdEl)
                    ? crIdEl.GetString() ?? string.Empty : string.Empty;

                var orderNumber = item.TryGetProperty("orderNumber", out var onEl)
                    ? onEl.GetString() ?? string.Empty : string.Empty;

                var status = item.TryGetProperty("status", out var stEl)
                    ? stEl.GetString() ?? string.Empty : string.Empty;

                var reason = item.TryGetProperty("reason", out var rsEl)
                    ? rsEl.GetString() ?? string.Empty : string.Empty;

                var reasonDetail = item.TryGetProperty("reasonDetail", out var rdEl)
                    ? rdEl.GetString() : null;

                var customerName = item.TryGetProperty("customerName", out var cnEl)
                    ? cnEl.GetString() ?? string.Empty : string.Empty;

                var amount = 0m;
                if (item.TryGetProperty("amount", out var amtEl))
                    decimal.TryParse(amtEl.GetRawText(), NumberStyles.Any, CultureInfo.InvariantCulture, out amount);

                var claimDate = DateTime.UtcNow;
                if (item.TryGetProperty("requestDate", out var cdEl) &&
                    DateTime.TryParse(cdEl.GetString(), CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var parsedClaimDate))
                {
                    claimDate = parsedClaimDate;
                }

                var claim = new ExternalClaimDto
                {
                    PlatformClaimId = claimId,
                    PlatformCode = PlatformCode,
                    OrderNumber = orderNumber,
                    Status = status,
                    Reason = reason,
                    ReasonDetail = reasonDetail,
                    CustomerName = customerName,
                    Amount = amount,
                    Currency = "TRY",
                    ClaimDate = claimDate
                };

                // Parse line items if available
                if (item.TryGetProperty("lines", out var linesArr) && linesArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var lineEl in linesArr.EnumerateArray())
                    {
                        claim.Lines.Add(new ExternalClaimLineDto
                        {
                            SKU = lineEl.TryGetProperty("sku", out var skuEl) ? skuEl.GetString() : null,
                            Barcode = lineEl.TryGetProperty("barcode", out var bcEl) ? bcEl.GetString() : null,
                            ProductName = lineEl.TryGetProperty("productName", out var pnEl)
                                ? pnEl.GetString() ?? string.Empty : string.Empty,
                            Quantity = lineEl.TryGetProperty("quantity", out var qtyEl)
                                ? qtyEl.GetInt32() : 1,
                            UnitPrice = lineEl.TryGetProperty("unitPrice", out var upEl)
                                && decimal.TryParse(upEl.GetRawText(), NumberStyles.Any, CultureInfo.InvariantCulture, out var upVal)
                                ? upVal : 0m
                        });
                    }
                }

                claims.Add(claim);
            }

            _logger.LogInformation("PttAVM PullClaims: {Count} claims retrieved", claims.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM PullClaims failed");
        }

        return claims.AsReadOnly();
    }

    /// <summary>
    /// Approves a return request on PTT AVM.
    /// PUT /api/return-requests/{id}/approve
    /// </summary>
    public async Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            if (string.IsNullOrWhiteSpace(claimId))
            {
                _logger.LogWarning("PttAVM ApproveClaim — claimId bos olamaz");
                return false;
            }

            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var url = $"{_baseUrl}/api/return-requests/{claimId}/approve";
            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Put, url);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("PttAVM ApproveClaim basarili — ClaimId={ClaimId}", claimId);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("PttAVM ApproveClaim basarisiz {Status}: {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM ApproveClaim hatasi — ClaimId={ClaimId}", claimId);
            return false;
        }
    }

    /// <summary>
    /// Rejects a return request on PTT AVM.
    /// PUT /api/return-requests/{id}/reject with reason body.
    /// </summary>
    public async Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            if (string.IsNullOrWhiteSpace(claimId))
            {
                _logger.LogWarning("PttAVM RejectClaim — claimId bos olamaz");
                return false;
            }

            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var payload = new { reason };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}/api/return-requests/{claimId}/reject";

            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Put, url);
                    req.Content = content;
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("PttAVM RejectClaim basarili — ClaimId={ClaimId}, Reason={Reason}",
                    claimId, reason);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("PttAVM RejectClaim basarisiz {Status}: {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM RejectClaim hatasi — ClaimId={ClaimId}", claimId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // IInvoiceCapableAdapter — Fatura Gonderimi
    // ═══════════════════════════════════════════

    /// <summary>
    /// Sends an invoice link for an order on PTT AVM.
    /// POST /api/orders/{id}/invoice with invoiceUrl body.
    /// </summary>
    public async Task<bool> SendInvoiceLinkAsync(string shipmentPackageId, string invoiceUrl, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            if (string.IsNullOrWhiteSpace(shipmentPackageId))
            {
                _logger.LogWarning("PttAVM SendInvoiceLink — shipmentPackageId bos olamaz");
                return false;
            }

            if (string.IsNullOrWhiteSpace(invoiceUrl))
            {
                _logger.LogWarning("PttAVM SendInvoiceLink — invoiceUrl bos olamaz. PackageId={PackageId}",
                    shipmentPackageId);
                return false;
            }

            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var payload = new { invoiceUrl };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}/api/orders/{shipmentPackageId}/invoice";

            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post, url);
                    req.Content = content;
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "PttAVM SendInvoiceLink basarili — PackageId={PackageId}, Url={Url}",
                    shipmentPackageId, invoiceUrl);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("PttAVM SendInvoiceLink basarisiz {Status}: {Error}",
                response.StatusCode, error);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM SendInvoiceLink hatasi — PackageId={PackageId}", shipmentPackageId);
            return false;
        }
    }

    /// <summary>
    /// Uploads invoice PDF file for an order on PTT AVM.
    /// POST /api/orders/{id}/invoice-upload with multipart/form-data.
    /// </summary>
    public async Task<bool> SendInvoiceFileAsync(string shipmentPackageId, byte[] pdfBytes, string fileName, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            if (string.IsNullOrWhiteSpace(shipmentPackageId))
            {
                _logger.LogWarning("PttAVM SendInvoiceFile — shipmentPackageId bos olamaz");
                return false;
            }

            if (pdfBytes is null || pdfBytes.Length == 0)
            {
                _logger.LogWarning("PttAVM SendInvoiceFile — pdfBytes bos olamaz. PackageId={PackageId}",
                    shipmentPackageId);
                return false;
            }

            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            using var formContent = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(pdfBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            formContent.Add(fileContent, "file", fileName ?? "invoice.pdf");

            var url = $"{_baseUrl}/api/orders/{shipmentPackageId}/invoice-upload";

            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post, url);
                    req.Content = formContent;
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "PttAVM SendInvoiceFile basarili — PackageId={PackageId}, File={FileName}, Size={Size}bytes",
                    shipmentPackageId, fileName, pdfBytes.Length);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("PttAVM SendInvoiceFile basarisiz {Status}: {Error}",
                response.StatusCode, error);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PttAVM SendInvoiceFile hatasi — PackageId={PackageId}", shipmentPackageId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // IWebhookCapableAdapter
    // ═══════════════════════════════════════════

    public async Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PttAvmAdapter.RegisterWebhookAsync: {Url}", callbackUrl);

        try
        {
            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                callbackUrl,
                eventTypes = new[] { "ORDER_CREATED", "ORDER_UPDATED", "RETURN_CREATED", "SHIPMENT_UPDATED" }
            };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/webhook/register");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("PttAVM RegisterWebhook failed: {Status} {Error}",
                    response.StatusCode, error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PttAVM RegisterWebhook exception");
            return false;
        }
    }

    public async Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PttAvmAdapter.UnregisterWebhookAsync");

        try
        {
            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/api/webhook/unregister");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("PttAVM UnregisterWebhook failed: {Status} {Error}",
                    response.StatusCode, error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PttAVM UnregisterWebhook exception");
            return false;
        }
    }

    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var eventType = doc.RootElement.TryGetProperty("eventType", out var et) ? et.GetString() : "unknown";

            _logger.LogInformation(
                "PttAvmAdapter webhook processed: EventType={EventType} PayloadLength={Length}",
                eventType, payload.Length);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[PttAvm] Malformed webhook payload ({Length}b)", payload?.Length ?? 0);
        }
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
            using var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);

            _logger.LogDebug("PttAVM ping: {StatusCode}", response.StatusCode);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "PttAVM ping failed");
            return false;
        }
    }

}
