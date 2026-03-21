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
/// eBay platform adaptoru — TAM implementasyon (H32 FINAL).
/// OAuth2 Client Credentials Grant token (cached, 5-min buffer).
/// Implements IIntegratorAdapter + IOrderCapableAdapter + IShipmentCapableAdapter.
/// Sell Inventory API, Fulfillment API, Shipping Fulfillment API, Commerce Taxonomy API.
/// </summary>
public class EbayAdapter : IIntegratorAdapter, IOrderCapableAdapter, IShipmentCapableAdapter, IPingableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EbayAdapter> _logger;
    private readonly EbayOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    // OAuth2 Client Credentials state
    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;
    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private string _tokenEndpoint;
    private bool _isConfigured;

    // eBay API base URL — initialised from options, overridable at runtime via credentials
    private string _ebayBaseUrl;

    // 5-minute safety buffer before actual expiry
    private static readonly TimeSpan TokenBuffer = TimeSpan.FromMinutes(5);

    public EbayAdapter(HttpClient httpClient, ILogger<EbayAdapter> logger,
        IOptions<EbayOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new EbayOptions();

        // Initialise from options so callers don't need to supply BaseUrl via credentials
        _ebayBaseUrl = _options.BaseUrl;
        _tokenEndpoint = _options.TokenUrl;

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
                        "[EbayAdapter] API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[EbayAdapter] Circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public string PlatformCode => nameof(PlatformType.eBay);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ═══════════════════════════════════════════
    // OAuth2 Client Credentials Token Management
    // ═══════════════════════════════════════════

    /// <summary>
    /// Cached OAuth2 Client Credentials Grant token with 5-minute buffer.
    /// Sets Authorization header on _httpClient after refresh.
    /// </summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry - TokenBuffer)
            return _accessToken;

        var credentials = Convert.ToBase64String(
            System.Text.Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = _options.OAuthScope
        });

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
            cancellationToken: ct).ConfigureAwait(false);

        _accessToken = json.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

        // Set Bearer token on default headers for subsequent calls
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);

        _logger.LogInformation("eBay OAuth2 token refreshed — expires in {Seconds}s", expiresIn);
        return _accessToken;
    }

    private void ConfigureCredentials(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _clientId = credentials.GetValueOrDefault("ClientId", string.Empty);
        _clientSecret = credentials.GetValueOrDefault("ClientSecret", string.Empty);

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("TokenEndpoint")))
            _tokenEndpoint = credentials["TokenEndpoint"];

        // Support BaseUrl override for sandbox testing
        // Use "https://api.sandbox.ebay.com" for eBay sandbox environment
        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _ebayBaseUrl = credentials["BaseUrl"].TrimEnd('/');

        // UseSandbox=true shortcut sets sandbox URL automatically
        if (credentials.GetValueOrDefault("UseSandbox", "false").Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            _ebayBaseUrl = _options.SandboxBaseUrl;
            _tokenEndpoint = $"{_options.SandboxBaseUrl}/identity/v1/oauth2/token";
        }

        _isConfigured = !string.IsNullOrWhiteSpace(_clientId) &&
                        !string.IsNullOrWhiteSpace(_clientSecret);
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "EbayAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
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
                result.ErrorMessage = "eBay: ClientId veya ClientSecret eksik";
                return result;
            }

            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);
            result.IsSuccess = !string.IsNullOrEmpty(token);
            result.StoreName = "eBay Store (OAuth2 OK)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eBay TestConnectionAsync başarısız");
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
    /// Pulls inventory items from eBay Sell Inventory API with pagination.
    /// GET /sell/inventory/v1/inventory_item?limit=100&amp;offset={offset}
    /// </summary>
    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EbayAdapter.PullProductsAsync called");

        var products = new List<Product>();

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            const int pageSize = 100;
            var offset = 0;
            var hasMore = true;

            while (hasMore)
            {
                var url = $"{_ebayBaseUrl}/sell/inventory/v1/inventory_item?limit={pageSize}&offset={offset}";
                var response = await _retryPipeline.ExecuteAsync(
                    async token => await _httpClient.GetAsync(url, token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("eBay PullProducts failed: {Status} - {Error}", response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("inventoryItems", out var items))
                    break;

                var pageCount = 0;
                foreach (var item in items.EnumerateArray())
                {
                    pageCount++;
                    var sku = item.TryGetProperty("sku", out var skuEl) ? skuEl.GetString() ?? "" : "";
                    var title = "";
                    var price = 0m;
                    var stock = 0;

                    if (item.TryGetProperty("product", out var productEl))
                    {
                        title = productEl.TryGetProperty("title", out var titleEl)
                            ? titleEl.GetString() ?? ""
                            : "";
                    }

                    if (item.TryGetProperty("availability", out var avail) &&
                        avail.TryGetProperty("shipToLocationAvailability", out var shipAvail) &&
                        shipAvail.TryGetProperty("quantity", out var qtyEl))
                    {
                        stock = qtyEl.GetInt32();
                    }

                    products.Add(new Product
                    {
                        Name = title,
                        SKU = sku,
                        Stock = stock,
                        SalePrice = 0m
                    });
                }

                offset += pageSize;
                // eBay returns "total" to indicate total count — stop if we've fetched all
                var total = doc.RootElement.TryGetProperty("total", out var totalEl) ? totalEl.GetInt32() : 0;
                hasMore = pageCount == pageSize && offset < total;
            }

            _logger.LogInformation("eBay PullProducts: {Count} products retrieved", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eBay PullProducts failed");
        }

        return products.AsReadOnly();
    }

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        // Full eBay listing creation requires multi-step flow:
        //   1. PUT /sell/inventory/v1/inventory_item/{sku} — create/update inventory item
        //   2. POST /sell/inventory/v1/offer — create offer with marketplace, price, listing policies
        //   3. POST /sell/inventory/v1/offer/{offerId}/publish — publish the offer as a live listing
        // Each step requires specific eBay listing policies (payment, return, fulfillment).
        // Use PushStockUpdateAsync / PushPriceUpdateAsync for existing listing updates.
        _logger.LogWarning(
            "EbayAdapter.PushProductAsync — full listing creation requires inventory+offer+publish flow (3-step). " +
            "SKU={SKU}. Use PushStockUpdateAsync/PushPriceUpdateAsync for existing listings",
            product.SKU);
        return Task.FromResult(false);
    }

    /// <summary>
    /// Updates available quantity for a SKU.
    /// PUT /sell/inventory/v1/inventory_item/{sku} — updates shipToLocationAvailability.quantity
    /// </summary>
    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EbayAdapter.PushStockUpdateAsync: ProductId={ProductId} qty={Qty}", productId, newStock);

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var sku = Uri.EscapeDataString(productId.ToString());
            var url = $"{_ebayBaseUrl}/sell/inventory/v1/inventory_item/{sku}";

            // We need the current inventory_item first to do a proper PUT (partial update not supported)
            var getResponse = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(url, token).ConfigureAwait(false), ct).ConfigureAwait(false);
            string existingProductTitle = string.Empty;

            if (getResponse.IsSuccessStatusCode)
            {
                var getContent = await getResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var getDoc = JsonDocument.Parse(getContent);
                if (getDoc.RootElement.TryGetProperty("product", out var prod) &&
                    prod.TryGetProperty("title", out var titleEl))
                {
                    existingProductTitle = titleEl.GetString() ?? string.Empty;
                }
            }

            var payload = new
            {
                availability = new
                {
                    shipToLocationAvailability = new
                    {
                        quantity = newStock
                    }
                },
                product = new
                {
                    title = existingProductTitle
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var putResponse = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.PutAsync(url, content, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!putResponse.IsSuccessStatusCode)
            {
                var error = await putResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("eBay StockUpdate failed: {Status} - {Error}", putResponse.StatusCode, error);
                return false;
            }

            _logger.LogInformation("eBay StockUpdate success: SKU={SKU} qty={Qty}", productId, newStock);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eBay StockUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Updates the price of an offer for the given SKU.
    /// Flow: GET /sell/inventory/v1/offer?sku={sku} → find offerId → PUT /sell/inventory/v1/offer/{offerId}
    /// </summary>
    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EbayAdapter.PushPriceUpdateAsync: ProductId={ProductId} price={Price}", productId, newPrice);

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var sku = Uri.EscapeDataString(productId.ToString());

            // Step 1: Find offerId for the SKU
            var getOffersUrl = $"{_ebayBaseUrl}/sell/inventory/v1/offer?sku={sku}";
            var getResponse = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(getOffersUrl, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!getResponse.IsSuccessStatusCode)
            {
                var error = await getResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("eBay GetOffers failed: {Status} - {Error}", getResponse.StatusCode, error);
                return false;
            }

            var getContent = await getResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var getDoc = JsonDocument.Parse(getContent);

            if (!getDoc.RootElement.TryGetProperty("offers", out var offers) ||
                !offers.EnumerateArray().Any())
            {
                _logger.LogWarning("eBay PriceUpdate: no offers found for SKU={SKU}", productId);
                return false;
            }

            var firstOffer = offers.EnumerateArray().First();
            var offerId = firstOffer.TryGetProperty("offerId", out var offerIdEl)
                ? offerIdEl.GetString() ?? ""
                : "";

            if (string.IsNullOrEmpty(offerId))
            {
                _logger.LogWarning("eBay PriceUpdate: offerId not found for SKU={SKU}", productId);
                return false;
            }

            // Step 2: PUT the updated price
            var currency = firstOffer.TryGetProperty("pricingSummary", out var ps) &&
                           ps.TryGetProperty("price", out var priceEl) &&
                           priceEl.TryGetProperty("currency", out var currencyEl)
                ? currencyEl.GetString() ?? "USD"
                : "USD";

            var payload = new
            {
                pricingSummary = new
                {
                    price = new
                    {
                        value = newPrice.ToString("F2", CultureInfo.InvariantCulture),
                        currency
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var putUrl = $"{_ebayBaseUrl}/sell/inventory/v1/offer/{offerId}";
            using var putContent = new StringContent(json, Encoding.UTF8, "application/json");
            var putResponse = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.PutAsync(putUrl, putContent, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!putResponse.IsSuccessStatusCode)
            {
                var error = await putResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("eBay PriceUpdate PUT failed: {Status} - {Error}", putResponse.StatusCode, error);
                return false;
            }

            _logger.LogInformation("eBay PriceUpdate success: SKU={SKU} price={Price} {Currency}",
                productId, newPrice, currency);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eBay PriceUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Retrieves the eBay category tree using the Commerce Taxonomy API.
    /// GET /commerce/taxonomy/v1/category_tree/{category_tree_id}
    /// category_tree_id=3 → Turkey (eBay TR marketplace).
    /// Recursively parses root → child categories into CategoryDto tree.
    /// </summary>
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EbayAdapter.GetCategoriesAsync called");

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            // category_tree_id=3 is Turkey; override via CategoryTreeId config if needed
            const int categoryTreeId = 3;
            var url = $"{_ebayBaseUrl}/commerce/taxonomy/v1/category_tree/{categoryTreeId}";
            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(url, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("eBay GetCategories failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<CategoryDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var categories = new List<CategoryDto>();

            // eBay Taxonomy response: { "categoryTreeId": "3", "rootCategoryNode": { ... } }
            if (doc.RootElement.TryGetProperty("rootCategoryNode", out var rootNode))
            {
                // Root node has "childCategoryTreeNodes" array
                if (rootNode.TryGetProperty("childCategoryTreeNodes", out var childNodes))
                {
                    foreach (var childNode in childNodes.EnumerateArray())
                    {
                        categories.Add(ParseEbayCategoryNode(childNode, parentId: null));
                    }
                }
                else
                {
                    // If no children, add the root itself
                    categories.Add(ParseEbayCategoryNode(rootNode, parentId: null));
                }
            }

            _logger.LogInformation("eBay GetCategories: {Count} top-level categories retrieved", categories.Count);
            return categories.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eBay GetCategories failed");
            return Array.Empty<CategoryDto>();
        }
    }

    /// <summary>
    /// Recursively parses an eBay category tree node into a CategoryDto.
    /// eBay node structure: { "category": { "categoryId": "123", "categoryName": "Electronics" },
    ///                        "childCategoryTreeNodes": [...] }
    /// </summary>
    private static CategoryDto ParseEbayCategoryNode(JsonElement node, int? parentId)
    {
        var categoryId = 0;
        var categoryName = string.Empty;

        if (node.TryGetProperty("category", out var catEl))
        {
            if (catEl.TryGetProperty("categoryId", out var idEl))
            {
                // eBay categoryId is a string in the API response
                var idStr = idEl.GetString() ?? "0";
                int.TryParse(idStr, out categoryId);
            }

            categoryName = catEl.TryGetProperty("categoryName", out var nameEl)
                ? nameEl.GetString() ?? string.Empty
                : string.Empty;
        }

        var dto = new CategoryDto
        {
            PlatformCategoryId = categoryId,
            Name = categoryName,
            ParentId = parentId
        };

        if (node.TryGetProperty("childCategoryTreeNodes", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                dto.SubCategories.Add(ParseEbayCategoryNode(child, parentId: categoryId));
            }
        }

        return dto;
    }

    // ═══════════════════════════════════════════
    // IOrderCapableAdapter — Orders
    // ═══════════════════════════════════════════

    /// <summary>
    /// Pulls orders from eBay Fulfillment API.
    /// GET /sell/fulfillment/v1/order?filter=lastmodifieddate:[{since}..] or creationdate
    /// </summary>
    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("EbayAdapter.PullOrdersAsync since={Since}", since);

        var orders = new List<ExternalOrderDto>();

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            // eBay date filter format: [2024-01-01T00:00:00.000Z..]
            var sinceDate = since ?? DateTime.UtcNow.AddDays(-30);
            var sinceStr = sinceDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            var filter = Uri.EscapeDataString($"creationdate:[{sinceStr}..]");

            const int pageSize = 50;
            var offset = 0;
            var hasMore = true;

            while (hasMore)
            {
                var url = $"{_ebayBaseUrl}/sell/fulfillment/v1/order?filter={filter}&limit={pageSize}&offset={offset}";
                var response = await _retryPipeline.ExecuteAsync(
                    async token => await _httpClient.GetAsync(url, token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("eBay PullOrders failed: {Status} - {Error}", response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (!doc.RootElement.TryGetProperty("orders", out var ordersArr))
                    break;

                var pageCount = 0;
                foreach (var orderEl in ordersArr.EnumerateArray())
                {
                    pageCount++;
                    var orderId = orderEl.TryGetProperty("orderId", out var oidEl) ? oidEl.GetString() ?? "" : "";
                    var orderStatus = orderEl.TryGetProperty("orderFulfillmentStatus", out var stEl)
                        ? stEl.GetString() ?? ""
                        : "";

                    var orderDate = DateTime.UtcNow;
                    if (orderEl.TryGetProperty("creationDate", out var createdEl) &&
                        DateTime.TryParse(createdEl.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var parsedDate))
                    {
                        orderDate = parsedDate;
                    }

                    DateTime? lastModified = null;
                    if (orderEl.TryGetProperty("lastModifiedDate", out var modEl) &&
                        DateTime.TryParse(modEl.GetString(), CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out var parsedMod))
                    {
                        lastModified = parsedMod;
                    }

                    var order = new ExternalOrderDto
                    {
                        PlatformCode = PlatformCode,
                        PlatformOrderId = orderId,
                        OrderNumber = orderId,
                        Status = orderStatus,
                        OrderDate = orderDate,
                        LastModifiedDate = lastModified,
                        Currency = "USD" // eBay default — overridden below if available
                    };

                    // Extract total price
                    if (orderEl.TryGetProperty("pricingSummary", out var pricing))
                    {
                        if (pricing.TryGetProperty("total", out var totalEl))
                        {
                            if (totalEl.TryGetProperty("value", out var valEl) &&
                                decimal.TryParse(valEl.GetString(), NumberStyles.Number,
                                    CultureInfo.InvariantCulture, out var totalAmount))
                            {
                                order.TotalAmount = totalAmount;
                            }
                            if (totalEl.TryGetProperty("currency", out var currEl))
                                order.Currency = currEl.GetString() ?? "USD";
                        }

                        if (pricing.TryGetProperty("deliveryCost", out var deliveryEl) &&
                            deliveryEl.TryGetProperty("value", out var dValEl) &&
                            decimal.TryParse(dValEl.GetString(), NumberStyles.Number,
                                CultureInfo.InvariantCulture, out var shippingCost))
                        {
                            order.ShippingCost = shippingCost;
                        }
                    }

                    // Extract buyer info
                    if (orderEl.TryGetProperty("buyer", out var buyer))
                    {
                        order.CustomerName = buyer.TryGetProperty("username", out var unEl)
                            ? unEl.GetString() ?? ""
                            : "";

                        if (buyer.TryGetProperty("taxAddress", out var taxAddr))
                        {
                            order.CustomerCity = taxAddr.TryGetProperty("city", out var cityEl)
                                ? cityEl.GetString()
                                : null;
                        }
                    }

                    // Extract shipping address
                    if (orderEl.TryGetProperty("fulfillmentStartInstructions", out var fulfillArr))
                    {
                        foreach (var instr in fulfillArr.EnumerateArray())
                        {
                            if (instr.TryGetProperty("shippingStep", out var shipStep) &&
                                shipStep.TryGetProperty("shipTo", out var shipTo))
                            {
                                if (shipTo.TryGetProperty("fullName", out var fnEl))
                                    order.CustomerName = fnEl.GetString() ?? order.CustomerName;

                                if (shipTo.TryGetProperty("primaryPhone", out var phoneEl) &&
                                    phoneEl.TryGetProperty("phoneNumber", out var phoneNum))
                                    order.CustomerPhone = phoneNum.GetString();

                                if (shipTo.TryGetProperty("contactAddress", out var addr))
                                {
                                    var street = addr.TryGetProperty("addressLine1", out var al1)
                                        ? al1.GetString() ?? ""
                                        : "";
                                    var city = addr.TryGetProperty("city", out var cityEl)
                                        ? cityEl.GetString() ?? ""
                                        : "";
                                    order.CustomerAddress = $"{street}, {city}".Trim(' ', ',');
                                    order.CustomerCity = string.IsNullOrEmpty(city) ? order.CustomerCity : city;
                                }
                                break;
                            }
                        }
                    }

                    // Extract order lines
                    if (orderEl.TryGetProperty("lineItems", out var lineItems))
                    {
                        foreach (var lineEl in lineItems.EnumerateArray())
                        {
                            var lineId = lineEl.TryGetProperty("lineItemId", out var liEl) ? liEl.GetString() : null;
                            var sku = lineEl.TryGetProperty("sku", out var skuEl) ? skuEl.GetString() : null;
                            var title = lineEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "";
                            var qty = lineEl.TryGetProperty("quantity", out var qtyEl) ? qtyEl.GetInt32() : 1;

                            var unitPrice = 0m;
                            if (lineEl.TryGetProperty("lineItemCost", out var licEl) &&
                                licEl.TryGetProperty("value", out var licValEl))
                            {
                                decimal.TryParse(licValEl.GetString(), NumberStyles.Number,
                                    CultureInfo.InvariantCulture, out unitPrice);
                                // lineItemCost is total for the line; divide by qty for unit price
                                if (qty > 0) unitPrice /= qty;
                            }

                            order.Lines.Add(new ExternalOrderLineDto
                            {
                                PlatformLineId = lineId,
                                SKU = sku,
                                ProductName = title,
                                Quantity = qty,
                                UnitPrice = unitPrice,
                                TaxRate = 0m, // eBay does not expose per-line tax rate in this endpoint
                                LineTotal = unitPrice * qty
                            });
                        }
                    }

                    orders.Add(order);
                }

                offset += pageSize;
                var total = doc.RootElement.TryGetProperty("total", out var totalCountEl) ? totalCountEl.GetInt32() : 0;
                hasMore = pageCount == pageSize && offset < total;
            }

            _logger.LogInformation("eBay PullOrders: {Count} orders retrieved", orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eBay PullOrders failed");
        }

        return orders.AsReadOnly();
    }

    /// <summary>
    /// eBay does not support direct order status updates via API.
    /// Order lifecycle is managed through shipping fulfillments (SendShipmentAsync) and eBay Resolution Center.
    /// Seller can mark shipped (via shipping_fulfillment POST) but cannot change order status directly.
    /// Always returns false.
    /// </summary>
    public Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default)
    {
        // eBay order status is driven by shipping fulfillments, not direct status updates.
        // To mark an order as shipped, use SendShipmentAsync which calls POST /shipping_fulfillment.
        // For cancellations, eBay requires the eBay Resolution Center or POST /sell/fulfillment/v1/order/{orderId}/cancel.
        _logger.LogWarning(
            "EbayAdapter.UpdateOrderStatusAsync — eBay does not support direct order status updates. " +
            "Use SendShipmentAsync for shipping or eBay Resolution Center for cancellations. " +
            "Package={PackageId} Status={Status}",
            packageId, status);
        return Task.FromResult(false);
    }

    // ═══════════════════════════════════════════
    // IShipmentCapableAdapter — Shipping Fulfillment
    // ═══════════════════════════════════════════

    /// <summary>
    /// Notifies eBay of a shipment for an order.
    /// POST /sell/fulfillment/v1/order/{orderId}/shipping_fulfillment
    /// </summary>
    public async Task<bool> SendShipmentAsync(
        string platformOrderId, string trackingNumber, CargoProvider provider,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation(
            "EbayAdapter.SendShipmentAsync: OrderId={OrderId} Tracking={Tracking} Provider={Provider}",
            platformOrderId, trackingNumber, provider);

        if (string.IsNullOrWhiteSpace(platformOrderId))
        {
            _logger.LogWarning("EbayAdapter.SendShipmentAsync — platformOrderId is required");
            return false;
        }

        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var carrierEnum = MapCargoProviderToEbayCarrier(provider);
            var encodedOrderId = Uri.EscapeDataString(platformOrderId);
            var url = $"{_ebayBaseUrl}/sell/fulfillment/v1/order/{encodedOrderId}/shipping_fulfillment";

            var payload = new
            {
                lineItems = Array.Empty<object>(), // empty = applies to all items in the order
                shippedDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                shippingCarrierCode = carrierEnum,
                trackingNumber
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.PostAsync(url, content, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("eBay SendShipment failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("eBay SendShipment success: OrderId={OrderId} Tracking={Tracking}",
                platformOrderId, trackingNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eBay SendShipment exception: OrderId={OrderId}", platformOrderId);
            return false;
        }
    }

    /// <summary>
    /// Maps internal CargoProvider enum to eBay shipping carrier code.
    /// eBay carrier codes: https://developer.ebay.com/devzone/guides/features-guide/default.html#Development/shipping-carriers.html
    /// </summary>
    private static string MapCargoProviderToEbayCarrier(CargoProvider provider) => provider switch
    {
        CargoProvider.UPS => "UPS",
        CargoProvider.MngKargo => "MNG",
        CargoProvider.ArasKargo => "ARAS_KARGO",
        CargoProvider.YurticiKargo => "YURTICI_KARGO",
        CargoProvider.SuratKargo => "SURAT_KARGO",
        CargoProvider.PttKargo => "PTT",
        CargoProvider.Hepsijet => "OTHER",
        _ => "OTHER"
    };

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
                new Uri(_ebayBaseUrl, UriKind.Absolute));
            var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);

            _logger.LogDebug("eBay ping: {StatusCode}", response.StatusCode);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "eBay ping failed");
            return false;
        }
    }
}
