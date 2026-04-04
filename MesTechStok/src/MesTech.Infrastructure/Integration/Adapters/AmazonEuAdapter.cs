using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Platform;
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
/// Amazon EU (SP-API) platform adapter — supports 7 EU marketplaces.
/// Forked from AmazonTrAdapter with multi-marketplace support.
/// IIntegratorAdapter + IOrderCapableAdapter
/// LWA OAuth2, Catalog, Orders, Feeds (XDocument — Stock + Price via SP-API Feeds).
/// SP-API endpoint: sellingpartnerapi-eu.amazon.com
///
/// Supported EU marketplace IDs:
///   DE = A1PA6795UKMFR9
///   FR = A13V1IB3VIYZZH
///   IT = APJ6JRA9NG5V4
///   ES = A1RKKUPIHCS9HS
///   NL = A1805IZSGTT6HS
///   SE = A2NODRKZP88ZB9
///   PL = A1C3SOZRARQ6R3
/// </summary>
public sealed class AmazonEuAdapter : IIntegratorAdapter, IOrderCapableAdapter, IShipmentCapableAdapter, IPingableAdapter,
    ISettlementCapableAdapter, IClaimCapableAdapter, IInvoiceCapableAdapter, IWebhookCapableAdapter,
    IReviewCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AmazonEuAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(30, 30);

    // LWA Auth State
    private string _refreshToken = string.Empty;
    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;
    private string _sellerId = string.Empty;
    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private string _lwaEndpoint;
    private string _baseUrl;
    private bool _isConfigured;

    // Active marketplace — configurable, defaults to DE
    private string _activeMarketplaceId = MarketplaceDE;

    // Constants
    private const string UnauthorizedStatusCode = "401";

    // EU Marketplace IDs
    private const string MarketplaceDE = "A1PA6795UKMFR9";
    private const string MarketplaceFR = "A13V1IB3VIYZZH";
    private const string MarketplaceIT = "APJ6JRA9NG5V4";
    private const string MarketplaceES = "A1RKKUPIHCS9HS";
    private const string MarketplaceNL = "A1805IZSGTT6HS";

    /// <summary>EU marketplace ID sabitleri — test ve external erisim icin.</summary>
    public static class MarketplaceIds
    {
        public const string DE = MarketplaceDE;
        public const string FR = MarketplaceFR;
        public const string IT = MarketplaceIT;
        public const string ES = MarketplaceES;
        public const string NL = MarketplaceNL;
    }
    private const string MarketplaceSE = "A2NODRKZP88ZB9";
    private const string MarketplacePL = "A1C3SOZRARQ6R3";

    /// <summary>
    /// All supported EU marketplace IDs for multi-marketplace queries.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> EuMarketplaces =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DE"] = MarketplaceDE,
            ["FR"] = MarketplaceFR,
            ["IT"] = MarketplaceIT,
            ["ES"] = MarketplaceES,
            ["NL"] = MarketplaceNL,
            ["SE"] = MarketplaceSE,
            ["PL"] = MarketplacePL
        };

    public AmazonEuAdapter(HttpClient httpClient, ILogger<AmazonEuAdapter> logger,
        IOptions<AmazonOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var opts = options?.Value ?? new AmazonOptions();
        _httpClient.Timeout = TimeSpan.FromSeconds(opts.HttpTimeoutSeconds);
        _lwaEndpoint = opts.LwaEndpoint;
        _baseUrl = opts.EuEndpoint;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            // HTTP 429 rate-limit retry — Amazon SP-API throttling with Retry-After
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
                    _logger.LogWarning("Amazon EU SP-API rate limited (429). Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("Amazon EU SP-API retry {Attempt} after {Delay}ms",
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

    public string PlatformCode => nameof(PlatformType.AmazonEu);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ===============================================
    // LWA OAuth2 Token Management
    // ===============================================

    private async Task EnsureFreshTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            return;

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = _refreshToken,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        });

        var lwaRequest = CreateRequest(HttpMethod.Post, _lwaEndpoint);
        lwaRequest.Content = content;
        using var response = await _httpClient.SendAsync(lwaRequest, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
            cancellationToken: ct).ConfigureAwait(false);

        _accessToken = json.RootElement.TryGetProperty("access_token", out var tokenProp)
            ? tokenProp.GetString() ?? throw new InvalidOperationException("Amazon LWA access_token is null")
            : throw new InvalidOperationException("Amazon LWA response missing access_token field");
        var expiresIn = json.RootElement.TryGetProperty("expires_in", out var expProp)
            ? expProp.GetInt32()
            : 3600; // fallback 1h if missing
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 60s buffer
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "MesTech-AmazonEU-Client/1.0");
        return request;
    }

    private async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(
        HttpMethod method, string path, CancellationToken ct)
    {
        await EnsureFreshTokenAsync(ct).ConfigureAwait(false);
        var request = CreateRequest(method, path);
        request.Headers.Add("x-amz-access-token", _accessToken);
        return request;
    }

    // ===============================================
    // Configure + TestConnection
    // ===============================================

    private void ConfigureAuth(Dictionary<string, string> credentials)
    {
        _refreshToken = credentials.GetValueOrDefault("RefreshToken", "")!;
        _clientId = credentials.GetValueOrDefault("ClientId", "")!;
        _clientSecret = credentials.GetValueOrDefault("ClientSecret", "")!;
        _sellerId = credentials.GetValueOrDefault("SellerId", "")!;

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _baseUrl = credentials["BaseUrl"];

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("LwaEndpoint")))
            _lwaEndpoint = credentials["LwaEndpoint"];

        // Allow selecting a specific EU marketplace (default: DE)
        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("MarketplaceCountry")) &&
            EuMarketplaces.TryGetValue(credentials["MarketplaceCountry"], out var mpId))
        {
            _activeMarketplaceId = mpId;
        }

        // SSRF guard (G106) — validate BaseUrl before assigning
        if (_httpClient.BaseAddress == null && !string.IsNullOrEmpty(_baseUrl))
        {
            if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var parsedUri) ||
                (parsedUri.Scheme != "https" && parsedUri.Scheme != "http"))
                throw new ArgumentException($"Invalid AmazonEu base URL scheme: {_baseUrl}. Only HTTP(S) allowed.");

            if (SsrfGuard.IsPrivateHost(parsedUri.Host))
                _logger.LogWarning("[AmazonEuAdapter] BaseUrl points to private network: {BaseUrl}", _baseUrl);

            _httpClient.BaseAddress = parsedUri;
        }

    }

    /// <summary>
    /// Returns a comma-separated list of all EU marketplace IDs for multi-marketplace queries.
    /// </summary>
    private static string AllEuMarketplaceIds()
        => string.Join(",", EuMarketplaces.Values);

    public async Task<ConnectionTestResultDto> TestConnectionAsync(
        Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var sw = Stopwatch.StartNew();
        var result = new ConnectionTestResultDto { PlatformCode = PlatformCode };

        try
        {
            // Credential validation
            if (!credentials.ContainsKey("RefreshToken") || string.IsNullOrWhiteSpace(credentials.GetValueOrDefault("RefreshToken")) ||
                !credentials.ContainsKey("ClientId") || string.IsNullOrWhiteSpace(credentials.GetValueOrDefault("ClientId")) ||
                !credentials.ContainsKey("ClientSecret") || string.IsNullOrWhiteSpace(credentials.GetValueOrDefault("ClientSecret")) ||
                !credentials.ContainsKey("SellerId") || string.IsNullOrWhiteSpace(credentials.GetValueOrDefault("SellerId")))
            {
                result.ErrorMessage = "RefreshToken, ClientId, ClientSecret ve SellerId alanlari zorunludur.";
                result.ResponseTime = sw.Elapsed;
                return result;
            }

            ConfigureAuth(credentials);

            // Get LWA token first
            await EnsureFreshTokenAsync(ct).ConfigureAwait(false);

            // Test: catalog items with limit=1 against the active EU marketplace
            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var req = await CreateAuthenticatedRequestAsync(
                        HttpMethod.Get,
                        $"/catalog/2022-04-01/items?marketplaceIds={_activeMarketplaceId}&includedData=summaries&pageSize=1",
                        token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;
            sw.Stop();
            result.ResponseTime = sw.Elapsed;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);
                var numberOfResults = doc.RootElement.TryGetProperty("numberOfResults", out var nr) ? nr.GetInt32() : 0;

                result.IsSuccess = true;
                result.ProductCount = numberOfResults;

                // Resolve the country code for display
                var countryLabel = EuMarketplaces
                    .FirstOrDefault(kv => kv.Value == _activeMarketplaceId).Key ?? "EU";
                result.StoreName = $"Amazon EU ({countryLabel}) - Seller {_sellerId}";
                _isConfigured = true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Yetkisiz erisim — LWA token veya credential hatali.",
                    System.Net.HttpStatusCode.Forbidden => "Erisim engellendi — SellerId veya marketplace hatali.",
                    _ => $"Amazon SP-API hatasi: {response.StatusCode} - {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex) when (ex.Message.Contains(UnauthorizedStatusCode) || ex.InnerException?.Message.Contains(UnauthorizedStatusCode) == true)
        {
            result.ErrorMessage = "Yetkisiz erisim — LWA token veya credential hatali.";
            result.ResponseTime = sw.Elapsed;
        }
        catch (HttpRequestException ex)
        {
            result.ErrorMessage = $"Baglanti hatasi: {ex.Message}";
            result.ResponseTime = sw.Elapsed;
        }
        catch (TaskCanceledException)
        {
            result.ErrorMessage = "Baglanti zaman asimina ugradi.";
            result.ResponseTime = sw.Elapsed;
        }
        catch (System.Text.Json.JsonException ex)
        {
            result.ErrorMessage = $"Gecersiz API yaniti: {ex.Message}";
            result.ResponseTime = sw.Elapsed;
        }

        _logger.LogInformation("Amazon EU connection test: Success={Success}, Time={Time}ms, Marketplace={Marketplace}",
            result.IsSuccess, result.ResponseTime.TotalMilliseconds, _activeMarketplaceId);
        return result;
    }

    // ===============================================
    // Catalog / Products
    // ===============================================

    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("AmazonEuAdapter.PullProductsAsync called — marketplace={Marketplace}", _activeMarketplaceId);

        var products = new List<Product>();

        try
        {
            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var req = await CreateAuthenticatedRequestAsync(
                        HttpMethod.Get,
                        $"/catalog/2022-04-01/items?marketplaceIds={_activeMarketplaceId}&includedData=summaries",
                        token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Amazon EU PullProducts failed: {Status} - {Error}", response.StatusCode, error);
                return products.AsReadOnly();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var asin = item.TryGetProperty("asin", out var a) ? a.GetString() ?? "" : "";
                    var title = "";
                    var sku = asin; // Default SKU to ASIN

                    // Extract title from summaries
                    if (item.TryGetProperty("summaries", out var summaries))
                    {
                        foreach (var summary in summaries.EnumerateArray())
                        {
                            if (summary.TryGetProperty("itemName", out var nameEl))
                                title = nameEl.GetString() ?? "";
                        }
                    }

                    // Extract SKU from identifiers
                    if (item.TryGetProperty("identifiers", out var identifiers))
                    {
                        foreach (var idGroup in identifiers.EnumerateArray())
                        {
                            if (idGroup.TryGetProperty("identifiers", out var ids))
                            {
                                foreach (var id in ids.EnumerateArray())
                                {
                                    if (id.TryGetProperty("identifierType", out var idType) &&
                                        idType.GetString() == "SKU" &&
                                        id.TryGetProperty("identifier", out var idVal))
                                    {
                                        sku = idVal.GetString() ?? asin;
                                    }
                                }
                            }
                        }
                    }

                    products.Add(new Product
                    {
                        Name = title,
                        SKU = sku,
                        Barcode = asin
                    });
                }
            }

            _logger.LogInformation("Amazon EU PullProducts: {Count} products retrieved", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon EU PullProducts failed");
        }

        return products.AsReadOnly();
    }

    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        EnsureConfigured();
        _logger.LogInformation("AmazonEuAdapter.PushProductAsync SKU: {SKU}", product.SKU);

        try
        {
            var payload = new
            {
                productType = "PRODUCT",
                patches = new[]
                {
                    new
                    {
                        op = "replace",
                        path = "/attributes/item_name",
                        value = new[] { new { value = product.Name, marketplace_id = _activeMarketplaceId } }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var sku = Uri.EscapeDataString(product.SKU);

            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var request = await CreateAuthenticatedRequestAsync(
                        HttpMethod.Put,
                        $"/listings/2021-08-01/items/{_sellerId}/{sku}?marketplaceIds={_activeMarketplaceId}",
                        token).ConfigureAwait(false);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Amazon EU PushProduct failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Amazon EU PushProduct success: {SKU}", product.SKU);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon EU PushProduct exception: {SKU}", product.SKU);
            return false;
        }
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("AmazonEuAdapter.GetCategoriesAsync — extracting classification nodes from catalog items for marketplace {Marketplace}", _activeMarketplaceId);

        try
        {
            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var req = await CreateAuthenticatedRequestAsync(
                        HttpMethod.Get,
                        $"/catalog/2022-04-01/items?marketplaceIds={_activeMarketplaceId}&includedData=classifications&pageSize=20",
                        token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Amazon EU GetCategories failed {Status}", response.StatusCode);
                return Array.Empty<CategoryDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var seen = new HashSet<string>();
            var categories = new List<CategoryDto>();

            if (doc.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (!item.TryGetProperty("classifications", out var classifs)) continue;
                    foreach (var c in classifs.EnumerateArray())
                    {
                        var displayName = c.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "";
                        var classId = c.TryGetProperty("classificationId", out var cid) ? cid.GetString() ?? "" : "";
                        if (string.IsNullOrEmpty(classId) || !seen.Add(classId)) continue;

                        categories.Add(new CategoryDto
                        {
                            PlatformCategoryId = int.TryParse(classId, out var idVal) ? idVal : classId.GetHashCode(),
                            Name = displayName
                        });
                    }
                }
            }

            _logger.LogInformation("AmazonEu GetCategories: {Count} unique classification nodes for {Marketplace}",
                categories.Count, _activeMarketplaceId);
            return categories.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AmazonEu GetCategories exception");
            return Array.Empty<CategoryDto>();
        }
    }

    // ===============================================
    // IOrderCapableAdapter — Orders (real implementation)
    // ===============================================

    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("AmazonEuAdapter.PullOrdersAsync since={Since} marketplace={Marketplace}", since, _activeMarketplaceId);

        var orders = new List<ExternalOrderDto>();

        try
        {
            var createdAfter = since?.ToString("o", CultureInfo.InvariantCulture) ??
                               DateTime.UtcNow.AddDays(-30).ToString("o", CultureInfo.InvariantCulture);

            var url = $"/orders/v0/orders?MarketplaceIds={_activeMarketplaceId}&CreatedAfter={Uri.EscapeDataString(createdAfter)}";

            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, url, token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Amazon EU PullOrders failed: {Status} - {Error}", response.StatusCode, error);
                return orders.AsReadOnly();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("payload", out var payload) &&
                payload.TryGetProperty("Orders", out var ordersArr))
            {
                foreach (var orderEl in ordersArr.EnumerateArray())
                {
                    var orderId = orderEl.TryGetProperty("AmazonOrderId", out var oid) ? oid.GetString() ?? "" : "";

                    // Detect currency from the order — EU orders use EUR, SEK, PLN etc.
                    var currency = "EUR";
                    if (orderEl.TryGetProperty("OrderTotal", out var orderTotal) &&
                        orderTotal.TryGetProperty("CurrencyCode", out var currEl))
                    {
                        currency = currEl.GetString() ?? "EUR";
                    }

                    var order = new ExternalOrderDto
                    {
                        PlatformCode = PlatformCode,
                        PlatformOrderId = orderId,
                        OrderNumber = orderId,
                        Status = orderEl.TryGetProperty("OrderStatus", out var st) ? st.GetString() ?? "" : "",
                        OrderDate = orderEl.TryGetProperty("PurchaseDate", out var pd)
                            ? DateTime.TryParse(pd.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate)
                                ? parsedDate
                                : DateTime.UtcNow
                            : DateTime.UtcNow,
                        Currency = currency
                    };

                    // Extract total amount
                    if (orderEl.TryGetProperty("OrderTotal", out var total) &&
                        total.TryGetProperty("Amount", out var amount))
                    {
                        if (decimal.TryParse(amount.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var totalAmount))
                            order.TotalAmount = totalAmount;
                    }

                    // Extract buyer info
                    if (orderEl.TryGetProperty("BuyerInfo", out var buyerInfo))
                    {
                        order.CustomerName = buyerInfo.TryGetProperty("BuyerName", out var bn) ? bn.GetString() ?? "" : "";
                        order.CustomerEmail = buyerInfo.TryGetProperty("BuyerEmail", out var be) ? be.GetString() : null;
                    }

                    // Fetch order items
                    await PopulateOrderItemsAsync(order, orderId, ct).ConfigureAwait(false);

                    orders.Add(order);
                }
            }

            _logger.LogInformation("Amazon EU PullOrders: {Count} orders retrieved", orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon EU PullOrders failed");
        }

        return orders.AsReadOnly();
    }

    private async Task PopulateOrderItemsAsync(ExternalOrderDto order, string orderId, CancellationToken ct)
    {
        try
        {
            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var request = await CreateAuthenticatedRequestAsync(
                        HttpMethod.Get, $"/orders/v0/orders/{orderId}/orderItems", token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("payload", out var payload) &&
                payload.TryGetProperty("OrderItems", out var itemsArr))
            {
                foreach (var item in itemsArr.EnumerateArray())
                {
                    var unitPrice = 0m;
                    if (item.TryGetProperty("ItemPrice", out var priceEl) &&
                        priceEl.TryGetProperty("Amount", out var priceAmount))
                    {
                        decimal.TryParse(priceAmount.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out unitPrice);
                    }

                    var qty = item.TryGetProperty("QuantityOrdered", out var qtyEl) ? qtyEl.GetInt32() : 1;

                    var taxAmount = 0m;
                    if (item.TryGetProperty("ItemTax", out var taxEl) &&
                        taxEl.TryGetProperty("Amount", out var taxAmount2))
                    {
                        decimal.TryParse(taxAmount2.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out taxAmount);
                    }

                    order.Lines.Add(new ExternalOrderLineDto
                    {
                        PlatformLineId = item.TryGetProperty("OrderItemId", out var oiid) ? oiid.GetString() : null,
                        SKU = item.TryGetProperty("SellerSKU", out var sku) ? sku.GetString() : null,
                        ProductName = item.TryGetProperty("Title", out var title) ? title.GetString() ?? "" : "",
                        Quantity = qty,
                        UnitPrice = unitPrice,
                        TaxRate = unitPrice > 0 ? taxAmount / unitPrice : 0.19m, // EU default VAT ~19% (DE)
                        LineTotal = unitPrice * qty
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Amazon EU PullOrderItems failed for order {OrderId}", orderId);
        }
    }

    public Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default)
    {
        // Amazon does not support direct order status update via Orders API
        _logger.LogWarning(
            "AmazonEuAdapter.UpdateOrderStatusAsync — not supported. Package={PackageId} Status={Status}",
            packageId, status);
        return Task.FromResult(false);
    }

    // ===============================================
    // Feeds — XDocument XML (Dalga 12)
    // ===============================================

    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation(
            "AmazonEuAdapter.PushStockUpdateAsync: ProductId={ProductId} qty={Qty} marketplace={Marketplace}",
            productId, newStock, _activeMarketplaceId);

        try
        {
            var feed = BuildInventoryFeed(productId.ToString(), newStock);
            return await SubmitFeedAsync(feed, "POST_INVENTORY_AVAILABILITY_DATA", ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon EU StockUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation(
            "AmazonEuAdapter.PushPriceUpdateAsync: ProductId={ProductId} price={Price} marketplace={Marketplace}",
            productId, newPrice, _activeMarketplaceId);

        try
        {
            var feed = BuildPricingFeed(productId.ToString(), newPrice);
            return await SubmitFeedAsync(feed, "POST_PRODUCT_PRICING_DATA", ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon EU PriceUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Resolves the ISO currency code for the active EU marketplace.
    /// DE/FR/IT/ES/NL → EUR, SE → SEK, PL → PLN.
    /// </summary>
    private string ResolveMarketplaceCurrency()
    {
        return _activeMarketplaceId switch
        {
            MarketplaceSE => "SEK",
            MarketplacePL => "PLN",
            _ => "EUR" // DE, FR, IT, ES, NL all use EUR
        };
    }

    /// <summary>
    /// Builds an Inventory feed XML using XDocument (LINQ to XML).
    /// Amazon Feeds API — POST_INVENTORY_AVAILABILITY_DATA.
    /// </summary>
    internal XDocument BuildInventoryFeed(string sku, int quantity)
    {
        var xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("AmazonEnvelope",
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute(xsi + "noNamespaceSchemaLocation", "amzn-envelope.xsd"),
                new XElement("Header",
                    new XElement("DocumentVersion", "1.01"),
                    new XElement("MerchantIdentifier", _sellerId)),
                new XElement("MessageType", "Inventory"),
                new XElement("Message",
                    new XElement("MessageID", "1"),
                    new XElement("OperationType", "Update"),
                    new XElement("Inventory",
                        new XElement("SKU", sku),
                        new XElement("Quantity", quantity)))));
    }

    /// <summary>
    /// Builds a Pricing feed XML using XDocument (LINQ to XML).
    /// Amazon Feeds API — POST_PRODUCT_PRICING_DATA.
    /// Currency is resolved from the active EU marketplace.
    /// </summary>
    internal XDocument BuildPricingFeed(string sku, decimal price)
    {
        var currency = ResolveMarketplaceCurrency();
        var xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("AmazonEnvelope",
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute(xsi + "noNamespaceSchemaLocation", "amzn-envelope.xsd"),
                new XElement("Header",
                    new XElement("DocumentVersion", "1.01"),
                    new XElement("MerchantIdentifier", _sellerId)),
                new XElement("MessageType", "Price"),
                new XElement("Message",
                    new XElement("MessageID", "1"),
                    new XElement("OperationType", "Update"),
                    new XElement("Price",
                        new XElement("SKU", sku),
                        new XElement("StandardPrice",
                            new XAttribute("currency", currency),
                            price.ToString("F2", CultureInfo.InvariantCulture))))));
    }

    /// <summary>
    /// Submits an XML feed to Amazon via the Feeds API (3-step flow).
    /// 1. POST /feeds/2021-06-30/documents → feedDocumentId + upload URL
    /// 2. PUT {uploadUrl} with XML content
    /// 3. POST /feeds/2021-06-30/feeds → create feed with feedDocumentId + feedType + EU marketplace
    /// </summary>
    private async Task<bool> SubmitFeedAsync(XDocument feedXml, string feedType, CancellationToken ct)
    {
        // Step 1: Create feed document
        var createDocPayload = JsonSerializer.Serialize(new { contentType = "text/xml; charset=UTF-8" }, _jsonOptions);

        var createDocResponse = await ThrottledExecuteAsync(
            async token =>
            {
                var request = await CreateAuthenticatedRequestAsync(
                    HttpMethod.Post, "/feeds/2021-06-30/documents", token).ConfigureAwait(false);
                request.Content = new StringContent(createDocPayload, Encoding.UTF8, "application/json");
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

        if (!createDocResponse.IsSuccessStatusCode)
        {
            var error = await createDocResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("Amazon EU CreateFeedDocument failed: {Status} - {Error}", createDocResponse.StatusCode, error);
            return false;
        }

        var createDocContent = await createDocResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var createDocJson = JsonDocument.Parse(createDocContent);
        var feedDocumentId = createDocJson.RootElement.TryGetProperty("feedDocumentId", out var fdId)
            ? fdId.GetString() : null;
        var uploadUrl = createDocJson.RootElement.TryGetProperty("url", out var urlProp)
            ? urlProp.GetString() : null;

        if (string.IsNullOrEmpty(feedDocumentId) || string.IsNullOrEmpty(uploadUrl))
        {
            _logger.LogError("Amazon EU CreateFeedDocument returned incomplete response: feedDocumentId={FdId}, url={Url}",
                feedDocumentId, uploadUrl);
            return false;
        }

        // Step 2: Upload XML to the pre-signed URL
        var xmlString = feedXml.Declaration != null
            ? feedXml.Declaration + Environment.NewLine + feedXml.ToString()
            : feedXml.ToString();

        var uploadRequest = CreateRequest(HttpMethod.Put, uploadUrl);
        uploadRequest.Content = new StringContent(xmlString, Encoding.UTF8, "text/xml");
        using var uploadResponse = await _httpClient.SendAsync(uploadRequest, ct).ConfigureAwait(false);

        if (!uploadResponse.IsSuccessStatusCode)
        {
            var uploadError = await uploadResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("Amazon EU feed XML upload failed: {Status} - {Error}", uploadResponse.StatusCode, uploadError);
            return false;
        }

        // Step 3: Create the feed — target the active EU marketplace
        var createFeedPayload = JsonSerializer.Serialize(new
        {
            feedType,
            marketplaceIds = new[] { _activeMarketplaceId },
            inputFeedDocumentId = feedDocumentId
        }, _jsonOptions);

        var createFeedResponse = await ThrottledExecuteAsync(
            async token =>
            {
                var request = await CreateAuthenticatedRequestAsync(
                    HttpMethod.Post, "/feeds/2021-06-30/feeds", token).ConfigureAwait(false);
                request.Content = new StringContent(createFeedPayload, Encoding.UTF8, "application/json");
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

        if (!createFeedResponse.IsSuccessStatusCode)
        {
            var error = await createFeedResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError("Amazon EU CreateFeed failed: {Status} - {Error}", createFeedResponse.StatusCode, error);
            return false;
        }

        var feedContent = await createFeedResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var feedJson = JsonDocument.Parse(feedContent);
        var feedId = feedJson.RootElement.TryGetProperty("feedId", out var fi) ? fi.GetString() : "unknown";

        _logger.LogInformation("Amazon EU feed submitted: FeedId={FeedId} Type={FeedType} Marketplace={Marketplace}",
            feedId, feedType, _activeMarketplaceId);
        return true;
    }

    // ===============================================
    // Guard
    // ===============================================

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "AmazonEuAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
    }

    // ═══════════════════════════════════════════
    // IShipmentCapableAdapter — Kargo Bildirimi
    // ═══════════════════════════════════════════

    public async Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber,
        CargoProvider provider, CancellationToken ct = default)
    {
        EnsureConfigured();

        var carrierCode = provider switch
        {
            CargoProvider.DHL => "DHL",
            CargoProvider.UPS => "UPS",
            CargoProvider.FedEx => "FedEx",
            CargoProvider.YurticiKargo => "Yurtici Kargo",
            CargoProvider.ArasKargo => "Aras Kargo",
            CargoProvider.MngKargo => "MNG Kargo",
            _ => provider.ToString()
        };

        _logger.LogInformation(
            "AmazonEuAdapter.SendShipmentAsync: OrderId={OrderId} Tracking={Tracking} Carrier={Carrier}",
            platformOrderId, trackingNumber, carrierCode);

        try
        {
            var feedXml = BuildShipmentConfirmFeed(platformOrderId, trackingNumber, carrierCode);

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    var req = CreateRequest(HttpMethod.Post, "/feeds/2021-06-30/feeds");
                    req.Content = new StringContent(feedXml, System.Text.Encoding.UTF8, "text/xml");
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("AmazonEu SendShipment failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("AmazonEu SendShipment OK: OrderId={OrderId}", platformOrderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AmazonEu SendShipment exception: OrderId={OrderId}", platformOrderId);
            return false;
        }
    }

    private static string BuildShipmentConfirmFeed(string orderId, string trackingNumber, string carrierCode)
    {
        var xsi = System.Xml.Linq.XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
        var doc = new System.Xml.Linq.XDocument(
            new System.Xml.Linq.XElement("AmazonEnvelope",
                new System.Xml.Linq.XAttribute(xsi + "noNamespaceSchemaLocation", "amzn-envelope.xsd"),
                new System.Xml.Linq.XElement("Header",
                    new System.Xml.Linq.XElement("DocumentVersion", "1.01"),
                    new System.Xml.Linq.XElement("MerchantIdentifier", "MERCHANT")),
                new System.Xml.Linq.XElement("MessageType", "OrderFulfillment"),
                new System.Xml.Linq.XElement("Message",
                    new System.Xml.Linq.XElement("MessageID", "1"),
                    new System.Xml.Linq.XElement("OrderFulfillment",
                        new System.Xml.Linq.XElement("AmazonOrderID", orderId),
                        new System.Xml.Linq.XElement("FulfillmentDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                        new System.Xml.Linq.XElement("FulfillmentData",
                            new System.Xml.Linq.XElement("CarrierName", carrierCode),
                            new System.Xml.Linq.XElement("ShipperTrackingNumber", trackingNumber))))));
        return doc.ToString();
    }

    // ═══════════════════════════════════════════
    // IReviewCapableAdapter — Product Reviews
    // ═══════════════════════════════════════════

    /// <summary>
    /// Gets product reviews from Amazon EU SP-API.
    /// Uses /products/reviews endpoint with seller-level pagination.
    /// </summary>
    public async Task<IReadOnlyList<TrendyolProductReviewDto>> GetProductReviewsAsync(
        int page = 0, int size = 20, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("AmazonEuAdapter.GetProductReviewsAsync page={Page} size={Size}", page, size);

        try
        {
            var response = await ThrottledExecuteAsync(async token =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"/products/reviews?sellerId={_sellerId}&pageSize={size}&pageToken={page}");
                request.Headers.TryAddWithoutValidation("x-amz-access-token", _accessToken);
                request.Headers.TryAddWithoutValidation("User-Agent", "MesTech-AmazonEU-Client/3.0");
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("AmazonEU GetProductReviews failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<TrendyolProductReviewDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var reviews = new List<TrendyolProductReviewDto>();
            var items = doc.RootElement.TryGetProperty("reviews", out var revArr) ? revArr
                : doc.RootElement.TryGetProperty("payload", out var payArr) ? payArr
                : doc.RootElement;

            if (items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    reviews.Add(new TrendyolProductReviewDto(
                        Id: item.TryGetProperty("reviewId", out var id) ? (id.ValueKind == JsonValueKind.Number ? id.GetInt64() : 0) : 0,
                        ProductId: item.TryGetProperty("asin", out var asin) ? asin.GetString()?.GetHashCode() ?? 0 : 0,
                        Comment: item.TryGetProperty("body", out var body) ? body.GetString() ?? ""
                            : item.TryGetProperty("text", out var text) ? text.GetString() ?? "" : "",
                        Rate: item.TryGetProperty("rating", out var rate) ? (int)rate.GetDouble() : 0,
                        UserFullName: item.TryGetProperty("reviewerName", out var name) ? name.GetString() ?? "" : "",
                        CreatedAt: item.TryGetProperty("date", out var dt)
                            ? (DateTime.TryParse(dt.GetString(), out var parsed) ? parsed : DateTime.MinValue)
                            : DateTime.MinValue,
                        IsReplied: item.TryGetProperty("isReplied", out var replied) && replied.GetBoolean()));
                }
            }

            _logger.LogInformation("AmazonEU GetProductReviews: {Count} reviews fetched", reviews.Count);
            return reviews;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "AmazonEU GetProductReviews exception");
            return Array.Empty<TrendyolProductReviewDto>();
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

            var request = CreateRequest(HttpMethod.Head, _baseUrl);
            using var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);

            _logger.LogDebug("Amazon EU ping: {StatusCode}", response.StatusCode);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Amazon EU ping failed");
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // ISettlementCapableAdapter — Financial Events (SP-API)
    // ═══════════════════════════════════════════

    /// <summary>
    /// GET /finances/v0/financialEvents — Amazon SP-API financial events for settlement.
    /// </summary>
    public async Task<SettlementDto?> GetSettlementAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            var postedAfter = startDate.ToUniversalTime().ToString("o");
            var postedBefore = endDate.ToUniversalTime().ToString("o");
            var url = $"/finances/v0/financialEvents?PostedAfter={Uri.EscapeDataString(postedAfter)}&PostedBefore={Uri.EscapeDataString(postedBefore)}";

            var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, url, ct).ConfigureAwait(false);

            using var response = await ThrottledExecuteAsync(async token =>
            {
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Amazon EU GetSettlement failed {Status}: {Error}", response.StatusCode, error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var settlement = new SettlementDto
            {
                PlatformCode = PlatformCode,
                StartDate = startDate,
                EndDate = endDate,
                Currency = "EUR"
            };

            // Parse payload.financialEvents.ShipmentEventList
            if (doc.RootElement.TryGetProperty("payload", out var payload) &&
                payload.TryGetProperty("financialEvents", out var events))
            {
                if (events.TryGetProperty("ShipmentEventList", out var shipments) &&
                    shipments.ValueKind == JsonValueKind.Array)
                {
                    foreach (var shipment in shipments.EnumerateArray())
                    {
                        var orderId = shipment.TryGetProperty("AmazonOrderId", out var oid) ? oid.GetString() : null;
                        var postedDate = shipment.TryGetProperty("PostedDate", out var pd) && pd.TryGetDateTime(out var pdt)
                            ? pdt : startDate;

                        if (shipment.TryGetProperty("ShipmentItemList", out var items) &&
                            items.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in items.EnumerateArray())
                            {
                                decimal itemAmount = 0m;
                                decimal commissionAmount = 0m;

                                if (item.TryGetProperty("ItemChargeList", out var charges) &&
                                    charges.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var charge in charges.EnumerateArray())
                                    {
                                        if (charge.TryGetProperty("ChargeAmount", out var ca) &&
                                            ca.TryGetProperty("CurrencyAmount", out var amount))
                                        {
                                            itemAmount += amount.GetDecimal();
                                        }
                                    }
                                }

                                if (item.TryGetProperty("ItemFeeList", out var fees) &&
                                    fees.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var fee in fees.EnumerateArray())
                                    {
                                        if (fee.TryGetProperty("FeeAmount", out var fa) &&
                                            fa.TryGetProperty("CurrencyAmount", out var amount))
                                        {
                                            commissionAmount += Math.Abs(amount.GetDecimal());
                                        }
                                    }
                                }

                                settlement.TotalSales += itemAmount;
                                settlement.TotalCommission += commissionAmount;

                                settlement.Lines.Add(new SettlementLineDto
                                {
                                    OrderNumber = orderId,
                                    TransactionType = "Shipment",
                                    Amount = itemAmount,
                                    CommissionAmount = commissionAmount,
                                    TransactionDate = postedDate
                                });
                            }
                        }
                    }
                }

                // Parse RefundEventList for return deductions
                if (events.TryGetProperty("RefundEventList", out var refunds) &&
                    refunds.ValueKind == JsonValueKind.Array)
                {
                    foreach (var refund in refunds.EnumerateArray())
                    {
                        var orderId = refund.TryGetProperty("AmazonOrderId", out var oid) ? oid.GetString() : null;
                        var postedDate = refund.TryGetProperty("PostedDate", out var pd) && pd.TryGetDateTime(out var pdt)
                            ? pdt : startDate;

                        if (refund.TryGetProperty("ShipmentItemAdjustmentList", out var adjustments) &&
                            adjustments.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var adj in adjustments.EnumerateArray())
                            {
                                decimal refundAmount = 0m;

                                if (adj.TryGetProperty("ItemChargeAdjustmentList", out var chargeAdj) &&
                                    chargeAdj.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var charge in chargeAdj.EnumerateArray())
                                    {
                                        if (charge.TryGetProperty("ChargeAmount", out var ca) &&
                                            ca.TryGetProperty("CurrencyAmount", out var amount))
                                        {
                                            refundAmount += Math.Abs(amount.GetDecimal());
                                        }
                                    }
                                }

                                settlement.TotalReturnDeduction += refundAmount;

                                settlement.Lines.Add(new SettlementLineDto
                                {
                                    OrderNumber = orderId,
                                    TransactionType = "Refund",
                                    Amount = -refundAmount,
                                    TransactionDate = postedDate
                                });
                            }
                        }
                    }
                }
            }

            settlement.NetAmount = settlement.TotalSales - settlement.TotalCommission
                                   - settlement.TotalShippingCost - settlement.TotalReturnDeduction;

            _logger.LogInformation("Amazon EU GetSettlement: {LineCount} lines, net={NetAmount}",
                settlement.Lines.Count, settlement.NetAmount);
            return settlement;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Amazon EU GetSettlement exception for {StartDate}–{EndDate}", startDate, endDate);
            return null;
        }
    }

    /// <summary>
    /// Amazon EU cargo invoices — not available via SP-API; returns empty list.
    /// </summary>
    public Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(DateTime startDate, CancellationToken ct = default)
    {
        _logger.LogInformation("Amazon EU GetCargoInvoices: cargo invoice API not available via SP-API — returning empty list");
        return Task.FromResult<IReadOnlyList<CargoInvoiceDto>>(Array.Empty<CargoInvoiceDto>());
    }

    // ═══════════════════════════════════════════
    // IClaimCapableAdapter — Iade/Claim Yonetimi
    // ═══════════════════════════════════════════

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExternalClaimDto>> PullClaimsAsync(DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        var claims = new List<ExternalClaimDto>();

        try
        {
            await EnsureFreshTokenAsync(ct).ConfigureAwait(false);

            // Use Orders API to fetch return orders — Amazon SP-API returns via orders with ReturnStatus
            var url = $"/orders/v0/orders?MarketplaceIds={_activeMarketplaceId}&OrderStatuses=Returned,Cancelled&MaxResultsPerPage=50";
            if (since.HasValue)
                url += "&CreatedAfter=" + since.Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            else
                url += "&CreatedAfter=" + DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

            using var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var req = await CreateAuthenticatedRequestAsync(HttpMethod.Get, url, token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Amazon EU PullClaims failed: {Status} {Error}",
                    response.StatusCode, errorBody);
                return claims;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("payload", out var payload) &&
                payload.TryGetProperty("Orders", out var ordersEl))
            {
                foreach (var order in ordersEl.EnumerateArray())
                {
                    claims.Add(new ExternalClaimDto
                    {
                        PlatformClaimId = order.TryGetProperty("AmazonOrderId", out var oidEl) ? oidEl.GetString() ?? string.Empty : string.Empty,
                        PlatformCode = PlatformCode,
                        OrderNumber = order.TryGetProperty("AmazonOrderId", out var onEl) ? onEl.GetString() ?? string.Empty : string.Empty,
                        Status = order.TryGetProperty("OrderStatus", out var stEl) ? stEl.GetString() ?? string.Empty : string.Empty,
                        Reason = order.TryGetProperty("CancelReason", out var rsEl) ? rsEl.GetString() ?? string.Empty : "Return",
                        CustomerName = order.TryGetProperty("BuyerInfo", out var biEl) && biEl.TryGetProperty("BuyerName", out var bnEl)
                            ? bnEl.GetString() ?? string.Empty : string.Empty,
                        Amount = order.TryGetProperty("OrderTotal", out var otEl) && otEl.TryGetProperty("Amount", out var amEl)
                            && decimal.TryParse(amEl.GetString(), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out var amt) ? amt : 0m,
                        Currency = order.TryGetProperty("OrderTotal", out var ocEl) && ocEl.TryGetProperty("CurrencyCode", out var ccEl)
                            ? ccEl.GetString() ?? "EUR" : "EUR",
                        ClaimDate = order.TryGetProperty("PurchaseDate", out var pdEl) && pdEl.TryGetDateTime(out var pd) ? pd : DateTime.UtcNow
                    });
                }
            }

            _logger.LogInformation("Amazon EU PullClaims: {Count} claims fetched", claims.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon EU PullClaims exception");
        }

        return claims;
    }

    /// <inheritdoc />
    public Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    {
        // Amazon auto-processes most returns — manual approval is not supported via SP-API.
        _logger.LogInformation("Amazon EU ApproveClaimAsync: Amazon auto-processes returns. ClaimId={ClaimId} acknowledged", claimId);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    {
        // Amazon does not support claim rejection via SP-API — returns are auto-processed.
        _logger.LogWarning("Amazon EU RejectClaimAsync: Amazon does not support return rejection via API. ClaimId={ClaimId}, Reason={Reason}", claimId, reason);
        return Task.FromResult(false);
    }

    // ═══════════════════════════════════════════
    // IInvoiceCapableAdapter — Fatura Gonderme
    // ═══════════════════════════════════════════

    /// <inheritdoc />
    /// <remarks>
    /// Amazon SP-API uses feed-based invoice submission (UPLOAD_VAT_INVOICE).
    /// URL-only invoice links are not natively supported — logging and returning true.
    /// </remarks>
    public Task<bool> SendInvoiceLinkAsync(string shipmentPackageId, string invoiceUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation(
            "AmazonEuAdapter.SendInvoiceLinkAsync: Package={PackageId} — URL-only not natively supported by Amazon SP-API, skipping feed. InvoiceUrl={InvoiceUrl}",
            shipmentPackageId, invoiceUrl);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Submits PDF invoice via Amazon SP-API Feeds (UPLOAD_VAT_INVOICE feed type).
    /// Step 1: POST /feeds/2021-06-30/documents to create feed document.
    /// Step 2: Upload PDF to the pre-signed URL.
    /// Step 3: POST /feeds/2021-06-30/feeds to create feed referencing the document.
    /// </remarks>
    public async Task<bool> SendInvoiceFileAsync(string shipmentPackageId, byte[] pdfBytes, string fileName, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("AmazonEuAdapter.SendInvoiceFileAsync: Package={PackageId} File={FileName}",
            shipmentPackageId, fileName);

        try
        {
            // Step 1: Create feed document
            var createDocPayload = new { contentType = "application/pdf" };
            var createDocJson = JsonSerializer.Serialize(createDocPayload, _jsonOptions);

            var createDocRequest = await CreateAuthenticatedRequestAsync(
                HttpMethod.Post, "/feeds/2021-06-30/documents", ct).ConfigureAwait(false);
            createDocRequest.Content = new StringContent(createDocJson, Encoding.UTF8, "application/json");

            var createDocResponse = await ThrottledExecuteAsync(async token =>
                await _httpClient.SendAsync(createDocRequest, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!createDocResponse.IsSuccessStatusCode)
            {
                var error = await createDocResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Amazon EU CreateFeedDocument failed: {Status} - {Error}",
                    createDocResponse.StatusCode, error);
                return false;
            }

            var docContent = await createDocResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var docJson = JsonDocument.Parse(docContent);
            var feedDocumentId = docJson.RootElement.TryGetProperty("feedDocumentId", out var docIdEl)
                ? docIdEl.GetString() : null;
            var uploadUrl = docJson.RootElement.TryGetProperty("url", out var urlEl)
                ? urlEl.GetString() : null;

            if (string.IsNullOrEmpty(feedDocumentId) || string.IsNullOrEmpty(uploadUrl))
            {
                _logger.LogError("Amazon EU CreateFeedDocument returned empty feedDocumentId or url");
                return false;
            }

            // Step 2: Upload PDF to the pre-signed URL
            using var uploadRequest = CreateRequest(HttpMethod.Put, uploadUrl);
            uploadRequest.Content = new ByteArrayContent(pdfBytes);
            uploadRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

            using var uploadResponse = await _httpClient.SendAsync(uploadRequest, ct).ConfigureAwait(false);
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var error = await uploadResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Amazon EU UploadInvoicePdf failed: {Status} - {Error}",
                    uploadResponse.StatusCode, error);
                return false;
            }

            // Step 3: Create feed referencing the document
            var feedPayload = new
            {
                feedType = "UPLOAD_VAT_INVOICE",
                marketplaceIds = new[] { _activeMarketplaceId },
                inputFeedDocumentId = feedDocumentId
            };
            var feedJson = JsonSerializer.Serialize(feedPayload, _jsonOptions);

            var feedRequest = await CreateAuthenticatedRequestAsync(
                HttpMethod.Post, "/feeds/2021-06-30/feeds", ct).ConfigureAwait(false);
            feedRequest.Content = new StringContent(feedJson, Encoding.UTF8, "application/json");

            var feedResponse = await ThrottledExecuteAsync(async token =>
                await _httpClient.SendAsync(feedRequest, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!feedResponse.IsSuccessStatusCode)
            {
                var error = await feedResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Amazon EU CreateFeed (UPLOAD_VAT_INVOICE) failed: {Status} - {Error}",
                    feedResponse.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Amazon EU SendInvoiceFile OK: Package={PackageId} FeedDocumentId={FeedDocumentId}",
                shipmentPackageId, feedDocumentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon EU SendInvoiceFile exception: Package={PackageId}", shipmentPackageId);
            return false;
        }
    }
    // ── IWebhookCapableAdapter (Amazon SNS) ──
    public Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default) { _logger.LogInformation("[AmazonEu] RegisterWebhook (SNS) {Url}", callbackUrl); return Task.FromResult(true); }
    public Task<bool> UnregisterWebhookAsync(CancellationToken ct = default) => Task.FromResult(true);
    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(payload)) return Task.CompletedTask;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var notificationType = root.TryGetProperty("NotificationType", out var nt) ? nt.GetString()
                                 : root.TryGetProperty("Type", out var tp) ? tp.GetString()
                                 : "unknown";
            var topicArn = root.TryGetProperty("TopicArn", out var ta) ? ta.GetString() : null;
            var messageId = root.TryGetProperty("MessageId", out var mid) ? mid.GetString() : null;
            _logger.LogInformation(
                "AmazonEU webhook processed: NotificationType={NotificationType} TopicArn={TopicArn} MessageId={MessageId} PayloadLength={Len}",
                notificationType, topicArn, messageId, payload.Length);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning(ex, "[AmazonEu] SNS webhook payload parse failed ({Len}b)", payload.Length);
        }
        return Task.CompletedTask;
    }
}
