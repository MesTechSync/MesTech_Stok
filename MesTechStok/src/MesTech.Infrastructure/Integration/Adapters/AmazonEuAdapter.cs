using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
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
public class AmazonEuAdapter : IIntegratorAdapter, IOrderCapableAdapter, IPingableAdapter
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
    private string _lwaEndpoint = "https://api.amazon.com/auth/o2/token";
    private string _baseUrl = EuEndpoint;
    private bool _isConfigured;

    // Active marketplace — configurable, defaults to DE
    private string _activeMarketplaceId = MarketplaceDE;

    // Constants
    private const string EuEndpoint = "https://sellingpartnerapi-eu.amazon.com";
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

    public AmazonEuAdapter(HttpClient httpClient, ILogger<AmazonEuAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                        "Amazon EU SP-API retry {Attempt} after {Delay}ms",
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

        var response = await _httpClient.PostAsync(_lwaEndpoint, content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
            cancellationToken: ct).ConfigureAwait(false);

        _accessToken = json.RootElement.GetProperty("access_token").GetString()!;
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 60s buffer
    }

    private async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(
        HttpMethod method, string path, CancellationToken ct)
    {
        await EnsureFreshTokenAsync(ct).ConfigureAwait(false);
        var request = new HttpRequestMessage(method, path);
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

        if (_httpClient.BaseAddress == null && !string.IsNullOrEmpty(_baseUrl))
            _httpClient.BaseAddress = new Uri(_baseUrl, UriKind.Absolute);

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MesTech-AmazonEU-Client/1.0");
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
            var response = await ThrottledExecuteAsync(
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
            var response = await ThrottledExecuteAsync(
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

            var response = await ThrottledExecuteAsync(
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
            var response = await ThrottledExecuteAsync(
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

            var response = await ThrottledExecuteAsync(
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
            var response = await ThrottledExecuteAsync(
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
        var feedDocumentId = createDocJson.RootElement.GetProperty("feedDocumentId").GetString()!;
        var uploadUrl = createDocJson.RootElement.GetProperty("url").GetString()!;

        // Step 2: Upload XML to the pre-signed URL
        var xmlString = feedXml.Declaration != null
            ? feedXml.Declaration + Environment.NewLine + feedXml.ToString()
            : feedXml.ToString();

        var uploadResponse = await _httpClient.PutAsync(
            uploadUrl,
            new StringContent(xmlString, Encoding.UTF8, "text/xml"),
            ct).ConfigureAwait(false);

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

            _logger.LogDebug("Amazon EU ping: {StatusCode}", response.StatusCode);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Amazon EU ping failed");
            return false;
        }
    }
}
