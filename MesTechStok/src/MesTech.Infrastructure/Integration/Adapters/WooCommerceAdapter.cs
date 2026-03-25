using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
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
/// WooCommerce platform adaptoru — TAM implementasyon (Dalga 10 C-02).
/// Auth: Basic Auth (ConsumerKey:ConsumerSecret → Base64).
/// API prefix: /wp-json/wc/v3/
/// PullProducts: GET products?per_page=100&amp;page=N&amp;status=publish (page-based pagination).
/// PushStockUpdate: GET products?sku={sku} → PUT products/{id} with stock_quantity.
/// PushPriceUpdate: GET products?sku={sku} → PUT products/{id} with regular_price.
/// GetOrders: GET orders?status=processing&amp;after=ISO_DATE (IOrderCapableAdapter).
/// </summary>
public sealed class WooCommerceAdapter : IIntegratorAdapter, IOrderCapableAdapter, IShipmentCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WooCommerceAdapter> _logger;
    private readonly WooCommerceOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);

    // Runtime credential state — set via TestConnectionAsync
    private string _siteUrl = string.Empty;
    private string _consumerKey = string.Empty;
    private string _consumerSecret = string.Empty;
    private bool _isConfigured;

    private const int PageSize = 100;

    public WooCommerceAdapter(HttpClient httpClient, ILogger<WooCommerceAdapter> logger,
        IOptions<WooCommerceOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new WooCommerceOptions();

        // Seed from options if provided
        if (!string.IsNullOrWhiteSpace(_options.SiteUrl))
        {
            _siteUrl = _options.SiteUrl.TrimEnd('/');
            _consumerKey = _options.ConsumerKey;
            _consumerSecret = _options.ConsumerSecret;
            _isConfigured = !string.IsNullOrWhiteSpace(_siteUrl) &&
                            !string.IsNullOrWhiteSpace(_consumerKey) &&
                            !string.IsNullOrWhiteSpace(_consumerSecret);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
                        "[WooCommerceAdapter] API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[WooCommerceAdapter] Circuit breaker OPENED for {Duration}s",
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

    public string PlatformCode => "WooCommerce";
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private string ApiBase => $"{_siteUrl}/wp-json/wc/v3";

    private void ConfigureCredentials(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        _siteUrl = credentials.GetValueOrDefault("SiteUrl", string.Empty).TrimEnd('/');
        _consumerKey = credentials.GetValueOrDefault("ConsumerKey", string.Empty);
        _consumerSecret = credentials.GetValueOrDefault("ConsumerSecret", string.Empty);

        _isConfigured = !string.IsNullOrWhiteSpace(_siteUrl) &&
                        !string.IsNullOrWhiteSpace(_consumerKey) &&
                        !string.IsNullOrWhiteSpace(_consumerSecret);

        if (_isConfigured)
            ApplyBasicAuthHeader();
    }

    private void ApplyBasicAuthHeader()
    {
        var token = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_consumerKey}:{_consumerSecret}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", token);
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "WooCommerceAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
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
                result.ErrorMessage = "WooCommerce: SiteUrl, ConsumerKey veya ConsumerSecret eksik";
                return result;
            }

            // System status endpoint — lightweight connectivity check
            var url = $"{ApiBase}/system_status";
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.GetAsync(url, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = $"HTTP {response.StatusCode}: {body}";
                return result;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("environment", out var env) &&
                env.TryGetProperty("site_url", out var siteEl))
            {
                result.StoreName = siteEl.GetString() ?? _siteUrl;
            }
            else
            {
                result.StoreName = _siteUrl;
            }

            // Product count
            var countUrl = $"{ApiBase}/products?per_page=1&page=1&status=publish";
            var countResponse = await ThrottledExecuteAsync(
                async token => await _httpClient.GetAsync(countUrl, token).ConfigureAwait(false), ct).ConfigureAwait(false);
            if (countResponse.IsSuccessStatusCode &&
                countResponse.Headers.TryGetValues("X-WP-Total", out var totalValues))
            {
                if (int.TryParse(totalValues.FirstOrDefault(), out var total))
                    result.ProductCount = total;
            }

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WooCommerce TestConnectionAsync basarisiz");
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
    /// Pulls all published products using page-based pagination.
    /// GET /wp-json/wc/v3/products?per_page=100&amp;page=N&amp;status=publish
    /// X-WP-TotalPages header drives loop termination.
    /// </summary>
    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("WooCommerceAdapter.PullProductsAsync called");

        var products = new List<Product>();

        try
        {
            var page = 1;
            int totalPages;

            do
            {
                var url = $"{ApiBase}/products?per_page={PageSize}&page={page}&status=publish";
                var response = await ThrottledExecuteAsync(
                    async token => await _httpClient.GetAsync(url, token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("WooCommerce PullProducts failed: {Status} - {Error}",
                        response.StatusCode, error);
                    break;
                }

                // Read total pages from header
                totalPages = 1;
                if (response.Headers.TryGetValues("X-WP-TotalPages", out var tpValues))
                    int.TryParse(tpValues.FirstOrDefault(), out totalPages);

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var wooProducts = JsonSerializer.Deserialize<List<WooProduct>>(content, _jsonOptions)
                                  ?? new List<WooProduct>();

                foreach (var p in wooProducts)
                {
                    products.Add(new Product
                    {
                        Name = p.Name ?? string.Empty,
                        SKU = p.Sku ?? string.Empty,
                        Stock = p.StockQuantity ?? 0,
                        SalePrice = decimal.TryParse(p.Price, NumberStyles.Number,
                            CultureInfo.InvariantCulture, out var price)
                            ? price
                            : 0m
                    });
                }

                page++;
            }
            while (page <= totalPages);

            _logger.LogInformation("WooCommerce PullProducts: {Count} products retrieved", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WooCommerce PullProducts failed");
        }

        return products.AsReadOnly();
    }

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "WooCommerceAdapter.PushProductAsync — tam urun olusturma desteği planlaniyor. SKU={SKU}",
            product.SKU);
        return Task.FromResult(false);
    }

    /// <summary>
    /// Updates stock_quantity for the WooCommerce product matching the given internal product ID (used as SKU).
    /// Flow: GET products?sku={id} → PUT products/{wooId} with stock_quantity.
    /// </summary>
    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("WooCommerceAdapter.PushStockUpdateAsync: ProductId={Id} qty={Qty}",
            productId, newStock);

        try
        {
            var sku = productId.ToString();

            // Search by SKU
            var searchUrl = $"{ApiBase}/products?sku={Uri.EscapeDataString(sku)}&per_page=1";
            var searchResponse = await ThrottledExecuteAsync(
                async token => await _httpClient.GetAsync(searchUrl, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!searchResponse.IsSuccessStatusCode)
            {
                var error = await searchResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("WooCommerce PushStockUpdate search failed: {Status} - {Error}",
                    searchResponse.StatusCode, error);
                return false;
            }

            var searchContent = await searchResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var found = JsonSerializer.Deserialize<List<WooProduct>>(searchContent, _jsonOptions)
                        ?? new List<WooProduct>();

            if (found.Count == 0)
            {
                _logger.LogWarning("WooCommerce PushStockUpdate: SKU={SKU} bulunamadi", sku);
                return false;
            }

            var wooId = found[0].Id;

            // PUT stock update
            var payload = JsonSerializer.Serialize(
                new { stock_quantity = newStock, manage_stock = true }, _jsonOptions);
            var putContent = new StringContent(payload, Encoding.UTF8, "application/json");

            var putUrl = $"{ApiBase}/products/{wooId}";
            var putResponse = await ThrottledExecuteAsync(
                async token => await _httpClient.PutAsync(putUrl, putContent, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!putResponse.IsSuccessStatusCode)
            {
                var error = await putResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("WooCommerce PushStockUpdate PUT failed: {Status} - {Error}",
                    putResponse.StatusCode, error);
                return false;
            }

            _logger.LogInformation("WooCommerce PushStockUpdate: SKU={SKU} → {Qty} OK", sku, newStock);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WooCommerce PushStockUpdate failed");
            return false;
        }
    }

    /// <summary>
    /// Updates regular_price for the WooCommerce product matching the given internal product ID (used as SKU).
    /// Flow: GET products?sku={id} → PUT products/{wooId} with regular_price.
    /// </summary>
    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("WooCommerceAdapter.PushPriceUpdateAsync: ProductId={Id} price={Price}",
            productId, newPrice);

        try
        {
            var sku = productId.ToString();

            // 1. Find product by SKU
            var searchUrl = $"{ApiBase}/products?sku={Uri.EscapeDataString(sku)}&per_page=1";
            var searchResponse = await ThrottledExecuteAsync(
                async token => await _httpClient.GetAsync(searchUrl, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!searchResponse.IsSuccessStatusCode)
            {
                var error = await searchResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("WooCommerce PushPriceUpdate search failed: {Status} - {Error}",
                    searchResponse.StatusCode, error);
                return false;
            }

            var searchContent = await searchResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var found = JsonSerializer.Deserialize<List<WooProduct>>(searchContent, _jsonOptions)
                        ?? new List<WooProduct>();

            if (found.Count == 0)
            {
                _logger.LogWarning("WooCommerce PushPriceUpdate: SKU={SKU} bulunamadi", sku);
                return false;
            }

            var wooId = found[0].Id;

            // 2. Update regular_price via PUT
            var payload = JsonSerializer.Serialize(
                new { regular_price = newPrice.ToString("F2", CultureInfo.InvariantCulture) }, _jsonOptions);
            var putContent = new StringContent(payload, Encoding.UTF8, "application/json");

            var putUrl = $"{ApiBase}/products/{wooId}";
            var putResponse = await ThrottledExecuteAsync(
                async token => await _httpClient.PutAsync(putUrl, putContent, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!putResponse.IsSuccessStatusCode)
            {
                var error = await putResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("WooCommerce PushPriceUpdate PUT failed: {Status} - {Error}",
                    putResponse.StatusCode, error);
                return false;
            }

            _logger.LogInformation("WooCommerce PushPriceUpdate: SKU={SKU} → {Price} OK", sku, newPrice);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WooCommerce PushPriceUpdate failed");
            return false;
        }
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var allCategories = new List<CategoryDto>();
        int page = 1;
        const int perPage = 100;

        while (true)
        {
            var response = await ThrottledExecuteAsync(async (token) =>
            {
                ApplyBasicAuthHeader();
                return await _httpClient.GetAsync(
                    $"{ApiBase}/products/categories?per_page={perPage}&page={page}", token)
                    .ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var categories = System.Text.Json.JsonSerializer.Deserialize<List<WcCategoryResponse>>(json, _jsonOptions);

            if (categories is null || categories.Count == 0) break;

            allCategories.AddRange(categories.Select(c => new CategoryDto
            {
                PlatformCategoryId = c.Id,
                Name = c.Name ?? string.Empty,
                ParentId = c.Parent > 0 ? c.Parent : null
            }));

            if (categories.Count < perPage) break;
            page++;
        }

        _logger.LogInformation("WooCommerce {Count} kategori çekildi", allCategories.Count);
        return allCategories;
    }

    private record WcCategoryResponse(int Id, string? Name, string? Slug, int Parent, int Count);

    // ─────────────────────────────────────────────
    // IOrderCapableAdapter
    // ─────────────────────────────────────────────

    /// <summary>
    /// Pulls orders with status=processing, optionally filtered by creation date.
    /// GET /wp-json/wc/v3/orders?status=processing&amp;after=ISO_DATE
    /// </summary>
    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("WooCommerceAdapter.PullOrdersAsync since={Since}", since);

        var orders = new List<ExternalOrderDto>();

        try
        {
            var after = since.HasValue
                ? $"&after={Uri.EscapeDataString(since.Value.ToString("o", CultureInfo.InvariantCulture))}"
                : string.Empty;

            var page = 1;
            int totalPages;

            do
            {
                var url = $"{ApiBase}/orders?status=processing&per_page={PageSize}&page={page}{after}";
                var response = await ThrottledExecuteAsync(
                    async token => await _httpClient.GetAsync(url, token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("WooCommerce PullOrders failed: {Status} - {Error}",
                        response.StatusCode, error);
                    break;
                }

                totalPages = 1;
                if (response.Headers.TryGetValues("X-WP-TotalPages", out var tpValues))
                    int.TryParse(tpValues.FirstOrDefault(), out totalPages);

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var wooOrders = JsonSerializer.Deserialize<List<WooOrder>>(content, _jsonOptions)
                                ?? new List<WooOrder>();

                foreach (var o in wooOrders)
                {
                    var dto = new ExternalOrderDto
                    {
                        PlatformOrderId = o.Id.ToString(CultureInfo.InvariantCulture),
                        PlatformCode = PlatformCode,
                        OrderNumber = o.Number ?? o.Id.ToString(CultureInfo.InvariantCulture),
                        Status = o.Status ?? string.Empty,
                        TotalAmount = decimal.TryParse(o.Total, NumberStyles.Number,
                            CultureInfo.InvariantCulture, out var total)
                            ? total
                            : 0m,
                        DiscountAmount = decimal.TryParse(o.DiscountTotal, NumberStyles.Number,
                            CultureInfo.InvariantCulture, out var disc)
                            ? disc
                            : null,
                        Currency = o.Currency ?? "USD",
                        OrderDate = DateTime.TryParse(o.DateCreated, out var createdDate)
                            ? createdDate
                            : DateTime.UtcNow
                    };

                    if (o.Billing is not null)
                    {
                        dto.CustomerName =
                            $"{o.Billing.FirstName} {o.Billing.LastName}".Trim();
                        dto.CustomerEmail = o.Billing.Email;
                        dto.CustomerPhone = o.Billing.Phone;
                        dto.CustomerAddress =
                            $"{o.Billing.Address1} {o.Billing.Address2}".Trim();
                        dto.CustomerCity = o.Billing.City;
                    }

                    if (o.LineItems is not null)
                    {
                        foreach (var li in o.LineItems)
                        {
                            dto.Lines.Add(new ExternalOrderLineDto
                            {
                                PlatformLineId = li.Id.ToString(CultureInfo.InvariantCulture),
                                SKU = li.Sku,
                                ProductName = li.Name ?? string.Empty,
                                Quantity = li.Quantity,
                                UnitPrice = decimal.TryParse(li.Price, NumberStyles.Number,
                                    CultureInfo.InvariantCulture, out var unitPrice)
                                    ? unitPrice
                                    : 0m,
                                LineTotal = decimal.TryParse(li.Total, NumberStyles.Number,
                                    CultureInfo.InvariantCulture, out var lineTotal)
                                    ? lineTotal
                                    : 0m,
                                TaxRate = 0m
                            });
                        }
                    }

                    orders.Add(dto);
                }

                page++;
            }
            while (page <= totalPages);

            _logger.LogInformation("WooCommerce PullOrders: {Count} orders retrieved", orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WooCommerce PullOrders failed");
        }

        return orders.AsReadOnly();
    }

    public async Task<bool> UpdateOrderStatusAsync(string packageId, string status,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("WooCommerceAdapter.UpdateOrderStatusAsync: OrderId={Id} Status={Status}",
            packageId, status);

        try
        {
            var payload = JsonSerializer.Serialize(new { status }, _jsonOptions);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var url = $"{ApiBase}/orders/{Uri.EscapeDataString(packageId)}";
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.PutAsync(url, content, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("WooCommerce UpdateOrderStatus failed: {Status} - {Error}",
                    response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WooCommerce UpdateOrderStatus failed");
            return false;
        }
    }

    // ─────────────────────────────────────────────
    // IShipmentCapableAdapter — Shipment
    // ─────────────────────────────────────────────

    /// <summary>
    /// Sends shipment notification to WooCommerce by updating order status to "completed"
    /// and attaching tracking metadata.
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
    /// Updates a WooCommerce order to "completed" status with tracking metadata.
    /// PUT /wp-json/wc/v3/orders/{orderId}
    /// Body: { "status": "completed", "meta_data": [{ "key": "_tracking_number", "value": "..." }, ...] }
    /// </summary>
    public async Task<bool> SendShipmentAsync(string platformOrderId, ShipmentInfoDto shipment,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(shipment);
        _logger.LogInformation(
            "WooCommerceAdapter.SendShipmentAsync: OrderId={OrderId} Tracking={Tracking}",
            platformOrderId, shipment.TrackingNumber);

        if (string.IsNullOrWhiteSpace(platformOrderId))
        {
            _logger.LogWarning("WooCommerceAdapter.SendShipmentAsync — platformOrderId gereklidir");
            return false;
        }

        try
        {
            var metaData = new List<object>
            {
                new { key = "_tracking_number", value = shipment.TrackingNumber },
                new { key = "_tracking_provider", value = shipment.TrackingCompany }
            };

            if (!string.IsNullOrWhiteSpace(shipment.TrackingUrl))
                metaData.Add(new { key = "_tracking_url", value = shipment.TrackingUrl });

            var payload = JsonSerializer.Serialize(new
            {
                status = "completed",
                meta_data = metaData
            });

            using var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");
            var url = $"{ApiBase}/orders/{Uri.EscapeDataString(platformOrderId)}";
            var response = await ThrottledExecuteAsync(
                async token => await _httpClient.PutAsync(url, requestContent, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("WooCommerce SendShipment failed: {Status} - {Error}",
                    response.StatusCode, error);
                return false;
            }

            _logger.LogInformation(
                "WooCommerce SendShipment success: OrderId={OrderId} Tracking={Tracking}",
                platformOrderId, shipment.TrackingNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WooCommerce SendShipment exception: OrderId={OrderId}", platformOrderId);
            return false;
        }
    }

    // ─────────────────────────────────────────────
    // Extended methods — Batch Update & Variations
    // ─────────────────────────────────────────────

    /// <summary>
    /// Batch updates products in WooCommerce (max 100 per request).
    /// POST /wp-json/wc/v3/products/batch
    /// Body: { "update": [{ "id": 1, "regular_price": "10.00", "stock_quantity": 5 }, ...] }
    /// </summary>
    public async Task<BatchUpdateResultDto> BatchUpdateProductsAsync(
        List<BatchProductUpdateDto> updates, CancellationToken ct = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(updates);
        _logger.LogInformation("WooCommerceAdapter.BatchUpdateProductsAsync: {Count} products", updates.Count);

        var result = new BatchUpdateResultDto();

        if (updates.Count == 0)
            return result;

        try
        {
            // WooCommerce batch API supports max 100 items per request
            const int batchSize = 100;
            var batches = new List<List<BatchProductUpdateDto>>();

            for (var i = 0; i < updates.Count; i += batchSize)
            {
                batches.Add(updates.GetRange(i, Math.Min(batchSize, updates.Count - i)));
            }

            foreach (var batch in batches)
            {
                var updateItems = new List<object>();

                foreach (var item in batch)
                {
                    var updateObj = new Dictionary<string, object> { { "id", item.ProductId } };

                    if (item.Price.HasValue)
                        updateObj["regular_price"] = item.Price.Value.ToString("F2", CultureInfo.InvariantCulture);

                    if (item.Stock.HasValue)
                    {
                        updateObj["stock_quantity"] = item.Stock.Value;
                        updateObj["manage_stock"] = true;
                    }

                    updateItems.Add(updateObj);
                }

                var payload = JsonSerializer.Serialize(new { update = updateItems });
                using var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");

                var url = $"{ApiBase}/products/batch";
                var response = await ThrottledExecuteAsync(
                    async token => await _httpClient.PostAsync(url, requestContent, token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("WooCommerce BatchUpdate failed: {Status} - {Error}",
                        response.StatusCode, error);
                    result.Errors.Add($"Batch failed: HTTP {(int)response.StatusCode}");
                    continue;
                }

                var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(responseContent);

                // Count updated items from response
                if (doc.RootElement.TryGetProperty("update", out var updatedArr))
                    result.Updated += updatedArr.GetArrayLength();
            }

            _logger.LogInformation(
                "WooCommerce BatchUpdate complete: {Updated} updated, {Errors} errors",
                result.Updated, result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WooCommerce BatchUpdateProducts failed");
            result.Errors.Add($"Exception: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Gets variations for a WooCommerce variable product.
    /// GET /wp-json/wc/v3/products/{productId}/variations?per_page=100
    /// </summary>
    public async Task<IReadOnlyList<ProductVariantDto>> GetProductVariationsAsync(
        string productId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("WooCommerceAdapter.GetProductVariationsAsync: ProductId={ProductId}", productId);

        if (string.IsNullOrWhiteSpace(productId))
        {
            _logger.LogWarning("WooCommerceAdapter.GetProductVariationsAsync — productId gereklidir");
            return Array.Empty<ProductVariantDto>();
        }

        try
        {
            var variations = new List<ProductVariantDto>();
            var page = 1;
            int totalPages;

            do
            {
                var url = $"{ApiBase}/products/{Uri.EscapeDataString(productId)}/variations?per_page={PageSize}&page={page}";
                var response = await ThrottledExecuteAsync(
                    async token => await _httpClient.GetAsync(url, token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("WooCommerce GetProductVariations failed: {Status} - {Error}",
                        response.StatusCode, error);
                    break;
                }

                totalPages = 1;
                if (response.Headers.TryGetValues("X-WP-TotalPages", out var tpValues))
                    int.TryParse(tpValues.FirstOrDefault(), out totalPages);

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                foreach (var vEl in doc.RootElement.EnumerateArray())
                {
                    variations.Add(new ProductVariantDto
                    {
                        VariantId = vEl.TryGetProperty("id", out var idEl)
                            ? idEl.GetInt64().ToString(CultureInfo.InvariantCulture)
                            : string.Empty,
                        Sku = vEl.TryGetProperty("sku", out var skuEl)
                            ? skuEl.GetString()
                            : null,
                        Title = vEl.TryGetProperty("description", out var descEl)
                            ? descEl.GetString()
                            : null,
                        Price = vEl.TryGetProperty("regular_price", out var priceEl)
                            ? priceEl.GetString()
                            : null,
                        StockQuantity = vEl.TryGetProperty("stock_quantity", out var sqEl)
                            && sqEl.ValueKind == JsonValueKind.Number
                            ? sqEl.GetInt32()
                            : 0,
                        ManageStock = vEl.TryGetProperty("manage_stock", out var msEl)
                            && msEl.ValueKind == JsonValueKind.True
                    });
                }

                page++;
            }
            while (page <= totalPages);

            _logger.LogInformation("WooCommerce GetProductVariations: {Count} variations for product {ProductId}",
                variations.Count, productId);
            return variations.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WooCommerce GetProductVariations failed: ProductId={ProductId}", productId);
            return Array.Empty<ProductVariantDto>();
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Options
// ─────────────────────────────────────────────────────────────────────────────

public sealed class WooCommerceOptions
{
    public const string Section = "Integrations:WooCommerce";

    /// <summary>WooCommerce site base URL, e.g. "https://mystore.com".</summary>
    public string SiteUrl { get; set; } = string.Empty;

    /// <summary>WooCommerce REST API Consumer Key (ck_...).</summary>
    public string ConsumerKey { get; set; } = string.Empty;

    /// <summary>WooCommerce REST API Consumer Secret (cs_...).</summary>
    public string ConsumerSecret { get; set; } = string.Empty;

    /// <summary>Whether the WooCommerce integration is enabled.</summary>
    public bool Enabled { get; set; } = false;
}

// ─────────────────────────────────────────────────────────────────────────────
// Internal response models (WooCommerce REST API v3)
// ─────────────────────────────────────────────────────────────────────────────

internal sealed class WooProduct
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("regular_price")]
    public string? RegularPrice { get; set; }

    [JsonPropertyName("sale_price")]
    public string? SalePrice { get; set; }

    [JsonPropertyName("stock_quantity")]
    public int? StockQuantity { get; set; }

    [JsonPropertyName("manage_stock")]
    public bool ManageStock { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

internal sealed class WooOrder
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("date_created")]
    public string? DateCreated { get; set; }

    [JsonPropertyName("date_modified")]
    public string? DateModified { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("total")]
    public string? Total { get; set; }

    [JsonPropertyName("discount_total")]
    public string? DiscountTotal { get; set; }

    [JsonPropertyName("shipping_total")]
    public string? ShippingTotal { get; set; }

    [JsonPropertyName("billing")]
    public WooBilling? Billing { get; set; }

    [JsonPropertyName("line_items")]
    public List<WooLineItem>? LineItems { get; set; }
}

internal sealed class WooBilling
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("address_1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("address_2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

internal sealed class WooLineItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }

    [JsonPropertyName("total")]
    public string? Total { get; set; }

    [JsonPropertyName("subtotal")]
    public string? Subtotal { get; set; }
}
