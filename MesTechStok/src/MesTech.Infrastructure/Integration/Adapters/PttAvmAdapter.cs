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
/// PTT AVM platform adaptoru — H31 real HTTP implementation.
/// Username + Password -> Bearer token exchange (cached, 5-min buffer).
/// Implements IIntegratorAdapter + IOrderCapableAdapter.
/// Orders: GET /api/orders
/// Stock: PUT /api/product/stock
/// Price: PUT /api/product/price
/// </summary>
public class PttAvmAdapter : IIntegratorAdapter, IOrderCapableAdapter, IPingableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PttAvmAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);

    // Username/Password -> Bearer token exchange
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private string _baseUrl;
    private string _tokenEndpoint;
    private bool _isConfigured;

    private const string DefaultBaseUrl = "https://apigw.pttavm.com";
    private const string DefaultTokenEndpoint = "https://apigw.pttavm.com/api/auth/login";

    // 5-minute safety buffer before actual expiry
    private static readonly TimeSpan TokenBuffer = TimeSpan.FromMinutes(5);

    public PttAvmAdapter(HttpClient httpClient, ILogger<PttAvmAdapter> logger,
        IOptions<PttAvmOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var opts = options?.Value;
        _baseUrl = opts?.BaseUrl ?? DefaultBaseUrl;
        _tokenEndpoint = opts?.TokenEndpoint ?? DefaultTokenEndpoint;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
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
                        "[PttAvmAdapter] API retry {Attempt} after {Delay}ms",
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

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
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

        // Set Bearer token on default headers for subsequent calls
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);

        _logger.LogInformation("PttAVM Bearer token refreshed — expires at {Expiry:u}", _tokenExpiry);
        return _accessToken;
    }

    private void ConfigureCredentials(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _username = credentials.GetValueOrDefault("Username", string.Empty);
        _password = credentials.GetValueOrDefault("Password", string.Empty);

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _baseUrl = credentials["BaseUrl"];
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

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        // POST /api/product/create — full listing creation requires category mapping
        _logger.LogWarning("PttAvmAdapter.PushProductAsync — full listing creation not yet implemented");
        return Task.FromResult(false);
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
                var response = await ThrottledExecuteAsync(
                    async token => await _httpClient.GetAsync(url, token).ConfigureAwait(false), ct).ConfigureAwait(false);

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
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.PutAsync(url, content, token).ConfigureAwait(false), ct).ConfigureAwait(false);

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
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.PutAsync(url, content, token).ConfigureAwait(false), ct).ConfigureAwait(false);

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
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.GetAsync(url, token).ConfigureAwait(false), ct).ConfigureAwait(false);

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
                var response = await ThrottledExecuteAsync(
                    async token => await _httpClient.GetAsync(url, token).ConfigureAwait(false), ct).ConfigureAwait(false);

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
