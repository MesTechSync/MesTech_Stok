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
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Amazon TR (SP-API) platform adaptoru — TAM entegrasyon.
/// IIntegratorAdapter + IOrderCapableAdapter
/// LWA OAuth2, Catalog, Orders, Feeds (XDocument), RDT, Notifications.
/// MarketplaceId: A33AVAJ2PDY3EV (Turkey)
/// </summary>
public sealed class AmazonTrAdapter : IIntegratorAdapter, IOrderCapableAdapter, IShipmentCapableAdapter,
    IPingableAdapter, ISettlementCapableAdapter, IClaimCapableAdapter, IInvoiceCapableAdapter,
    IWebhookCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AmazonTrAdapter> _logger;
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

    // Constants
    private const string TurkeyMarketplaceId = "A33AVAJ2PDY3EV";
    private const string UnauthorizedStatusCode = "401";

    public AmazonTrAdapter(HttpClient httpClient, ILogger<AmazonTrAdapter> logger,
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
                    _logger.LogWarning("Amazon TR SP-API rate limited (429). Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("Amazon SP-API retry {Attempt} after {Delay}ms",
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

    public string PlatformCode => nameof(PlatformType.Amazon);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ═══════════════════════════════════════════
    // LWA OAuth2 Token Management
    // ═══════════════════════════════════════════

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

        _accessToken = json.RootElement.TryGetProperty("access_token", out var tokenProp)
            ? tokenProp.GetString() ?? throw new InvalidOperationException("Amazon LWA access_token is null")
            : throw new InvalidOperationException("Amazon LWA response missing access_token field");
        var expiresIn = json.RootElement.TryGetProperty("expires_in", out var expProp)
            ? expProp.GetInt32()
            : 3600; // fallback 1h if missing
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 60s buffer
    }

    private async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(
        HttpMethod method, string path, CancellationToken ct)
    {
        await EnsureFreshTokenAsync(ct).ConfigureAwait(false);
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation("User-Agent", "MesTech-Amazon-Client/1.0");
        request.Headers.Add("x-amz-access-token", _accessToken);
        return request;
    }

    // ═══════════════════════════════════════════
    // Configure + TestConnection
    // ═══════════════════════════════════════════

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

        if (_httpClient.BaseAddress == null && !string.IsNullOrEmpty(_baseUrl))
            _httpClient.BaseAddress = new Uri(_baseUrl, UriKind.Absolute);

        // User-Agent is added per-request in CreateAuthenticatedRequestAsync for thread-safety
    }

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

            // Test: catalog items with limit=1
            var request = await CreateAuthenticatedRequestAsync(
                HttpMethod.Get,
                $"/catalog/2022-04-01/items?marketplaceIds={TurkeyMarketplaceId}&includedData=summaries&pageSize=1",
                ct).ConfigureAwait(false);

            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    // We need to clone the request on retry since HttpRequestMessage can only be sent once
                    var req = await CreateAuthenticatedRequestAsync(
                        HttpMethod.Get,
                        $"/catalog/2022-04-01/items?marketplaceIds={TurkeyMarketplaceId}&includedData=summaries&pageSize=1",
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
                result.StoreName = $"Amazon TR - Seller {_sellerId}";
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

        _logger.LogInformation("Amazon connection test: Success={Success}, Time={Time}ms",
            result.IsSuccess, result.ResponseTime.TotalMilliseconds);
        return result;
    }

    // ═══════════════════════════════════════════
    // Catalog / Products (Task 12)
    // ═══════════════════════════════════════════

    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("AmazonTrAdapter.PullProductsAsync called");

        var products = new List<Product>();

        try
        {
            var request = await CreateAuthenticatedRequestAsync(
                HttpMethod.Get,
                $"/catalog/2022-04-01/items?marketplaceIds={TurkeyMarketplaceId}&includedData=summaries",
                ct).ConfigureAwait(false);

            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var req = await CreateAuthenticatedRequestAsync(
                        HttpMethod.Get,
                        $"/catalog/2022-04-01/items?marketplaceIds={TurkeyMarketplaceId}&includedData=summaries",
                        token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Amazon PullProducts failed: {Status} - {Error}", response.StatusCode, error);
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

            _logger.LogInformation("Amazon PullProducts: {Count} products retrieved", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon PullProducts failed");
        }

        return products.AsReadOnly();
    }

    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        EnsureConfigured();
        _logger.LogInformation("AmazonTrAdapter.PushProductAsync SKU: {SKU}", product.SKU);

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
                        value = new[] { new { value = product.Name, marketplace_id = TurkeyMarketplaceId } }
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
                        $"/listings/2021-08-01/items/{_sellerId}/{sku}?marketplaceIds={TurkeyMarketplaceId}",
                        token).ConfigureAwait(false);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Amazon PushProduct failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Amazon PushProduct success: {SKU}", product.SKU);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon PushProduct exception: {SKU}", product.SKU);
            return false;
        }
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        // Amazon SP-API does not provide a "list all categories" endpoint.
        // Categories are ASIN-based browse nodes queried via GET /catalog/v0/categories?MarketplaceId=&ASIN=
        // A full category tree requires crawling browse nodes which is not practical for bulk sync.
        // Use GetCategoryAttributesAsync with a specific ASIN when category context is needed.
        EnsureConfigured();
        _logger.LogInformation("AmazonTrAdapter.GetCategoriesAsync — Amazon SP-API requires ASIN-based category lookup; returning top-level browse nodes");

        try
        {
            var request = await CreateAuthenticatedRequestAsync(
                HttpMethod.Get,
                $"/catalog/2022-04-01/items?marketplaceIds={TurkeyMarketplaceId}&includedData=classifications&pageSize=20",
                ct).ConfigureAwait(false);

            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var req = await CreateAuthenticatedRequestAsync(
                        HttpMethod.Get,
                        $"/catalog/2022-04-01/items?marketplaceIds={TurkeyMarketplaceId}&includedData=classifications&pageSize=20",
                        token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Amazon GetCategories failed {Status}", response.StatusCode);
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

            _logger.LogInformation("AmazonTr GetCategories: {Count} unique classification nodes extracted", categories.Count);
            return categories.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AmazonTr GetCategories exception");
            return Array.Empty<CategoryDto>();
        }
    }

    // ═══════════════════════════════════════════
    // IOrderCapableAdapter — Orders (Task 13)
    // ═══════════════════════════════════════════

    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("AmazonTrAdapter.PullOrdersAsync since={Since}", since);

        var orders = new List<ExternalOrderDto>();

        try
        {
            var createdAfter = since?.ToString("o", CultureInfo.InvariantCulture) ??
                               DateTime.UtcNow.AddDays(-30).ToString("o", CultureInfo.InvariantCulture);

            var url = $"/orders/v0/orders?MarketplaceIds={TurkeyMarketplaceId}&CreatedAfter={Uri.EscapeDataString(createdAfter)}";

            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, url, token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Amazon PullOrders failed: {Status} - {Error}", response.StatusCode, error);
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
                        Currency = "TRY"
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

            _logger.LogInformation("Amazon PullOrders: {Count} orders retrieved", orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon PullOrders failed");
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
                        TaxRate = unitPrice > 0 ? taxAmount / unitPrice : 0.18m,
                        LineTotal = unitPrice * qty
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Amazon PullOrderItems failed for order {OrderId}", orderId);
        }
    }

    public Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default)
    {
        // Amazon does not support direct order status update via Orders API
        _logger.LogWarning(
            "AmazonTrAdapter.UpdateOrderStatusAsync — not supported. Package={PackageId} Status={Status}",
            packageId, status);
        return Task.FromResult(false);
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
            CargoProvider.YurticiKargo => "Yurtici Kargo",
            CargoProvider.ArasKargo => "Aras Kargo",
            CargoProvider.SuratKargo => "Surat Kargo",
            CargoProvider.MngKargo => "MNG Kargo",
            CargoProvider.PttKargo => "PTT Posta",
            CargoProvider.UPS => "UPS",
            CargoProvider.DHL => "DHL",
            CargoProvider.FedEx => "FedEx",
            _ => provider.ToString()
        };

        _logger.LogInformation(
            "AmazonTrAdapter.SendShipmentAsync: OrderId={OrderId} Tracking={Tracking} Carrier={Carrier}",
            platformOrderId, trackingNumber, carrierCode);

        try
        {
            // Amazon SP-API: POST /orders/v0/orders/{orderId}/shipment/confirm
            // Uses OrderFulfillment feed (POST_ORDER_FULFILLMENT_DATA)
            var feedXml = BuildShipmentConfirmFeed(platformOrderId, trackingNumber, carrierCode);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(feedXml, Encoding.UTF8, "text/xml");
                    return await _httpClient.PostAsync(
                        new Uri("/feeds/2021-06-30/feeds", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("AmazonTr SendShipment failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("AmazonTr SendShipment OK: OrderId={OrderId}", platformOrderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AmazonTr SendShipment exception: OrderId={OrderId}", platformOrderId);
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
    // Feeds — XDocument XML (Task 14)
    // ═══════════════════════════════════════════

    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("AmazonTrAdapter.PushStockUpdateAsync: ProductId={ProductId} qty={Qty}", productId, newStock);

        try
        {
            var feed = BuildInventoryFeed(productId.ToString(), newStock);
            return await SubmitFeedAsync(feed, "POST_INVENTORY_AVAILABILITY_DATA", ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon StockUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("AmazonTrAdapter.PushPriceUpdateAsync: ProductId={ProductId} price={Price}", productId, newPrice);

        try
        {
            var feed = BuildPricingFeed(productId.ToString(), newPrice);
            return await SubmitFeedAsync(feed, "POST_PRODUCT_PRICING_DATA", ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Amazon PriceUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Builds an Inventory feed XML using XDocument (LINQ to XML).
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
    /// </summary>
    internal XDocument BuildPricingFeed(string sku, decimal price)
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
                new XElement("MessageType", "Price"),
                new XElement("Message",
                    new XElement("MessageID", "1"),
                    new XElement("OperationType", "Update"),
                    new XElement("Price",
                        new XElement("SKU", sku),
                        new XElement("StandardPrice",
                            new XAttribute("currency", "TRY"),
                            price.ToString("F2", CultureInfo.InvariantCulture))))));
    }

    /// <summary>
    /// Submits an XML feed to Amazon via the Feeds API (3-step flow).
    /// 1. POST /feeds/2021-06-30/documents → feedDocumentId + upload URL
    /// 2. PUT {uploadUrl} with XML content
    /// 3. POST /feeds/2021-06-30/feeds → create feed with feedDocumentId + feedType
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
            _logger.LogError("Amazon CreateFeedDocument failed: {Status} - {Error}", createDocResponse.StatusCode, error);
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
            _logger.LogError("Amazon feed XML upload failed: {Status} - {Error}", uploadResponse.StatusCode, uploadError);
            return false;
        }

        // Step 3: Create the feed
        var createFeedPayload = JsonSerializer.Serialize(new
        {
            feedType,
            marketplaceIds = new[] { TurkeyMarketplaceId },
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
            _logger.LogError("Amazon CreateFeed failed: {Status} - {Error}", createFeedResponse.StatusCode, error);
            return false;
        }

        var feedContent = await createFeedResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var feedJson = JsonDocument.Parse(feedContent);
        var feedId = feedJson.RootElement.TryGetProperty("feedId", out var fi) ? fi.GetString() : "unknown";

        _logger.LogInformation("Amazon feed submitted: FeedId={FeedId} Type={FeedType}", feedId, feedType);
        return true;
    }

    // ═══════════════════════════════════════════
    // RDT + Notifications (Task 15) — Private helpers
    // ═══════════════════════════════════════════

    /// <summary>
    /// Gets a Restricted Data Token for accessing PII data.
    /// </summary>
    private async Task<string?> GetRestrictedDataTokenAsync(
        string path, string[] dataElements, CancellationToken ct)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                restrictedResources = new[]
                {
                    new
                    {
                        method = "GET",
                        path,
                        dataElements
                    }
                }
            }, _jsonOptions);

            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var request = await CreateAuthenticatedRequestAsync(
                        HttpMethod.Post, "/tokens/2021-03-01/restrictedDataToken", token).ConfigureAwait(false);
                    request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("restrictedDataToken", out var rdt) ? rdt.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Amazon RDT request failed for path {Path}", path);
            return null;
        }
    }

    /// <summary>
    /// Creates a notification subscription for event-driven updates.
    /// </summary>
    private async Task<string?> CreateNotificationSubscriptionAsync(
        string notificationType, CancellationToken ct)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                payloadVersion = "1.0",
                destinationId = "default"
            }, _jsonOptions);

            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var request = await CreateAuthenticatedRequestAsync(
                        HttpMethod.Post, $"/notifications/v1/subscriptions/{notificationType}", token).ConfigureAwait(false);
                    request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("payload", out var pl) &&
                pl.TryGetProperty("subscriptionId", out var subId))
            {
                return subId.GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Amazon subscription creation failed for {Type}", notificationType);
            return null;
        }
    }

    // ═══════════════════════════════════════════
    // Guard
    // ═══════════════════════════════════════════

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "AmazonTrAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
    }

    // ═══════════════════════════════════════════
    // ISettlementCapableAdapter — Financial Events
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

            var response = await ThrottledExecuteAsync(async token =>
            {
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("AmazonTR GetSettlement failed {Status}: {Error}", response.StatusCode, error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var settlement = new SettlementDto
            {
                PlatformCode = "AmazonTR",
                StartDate = startDate,
                EndDate = endDate,
                Currency = "TRY"
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

            _logger.LogInformation("AmazonTR GetSettlement: {LineCount} lines, net={NetAmount}",
                settlement.Lines.Count, settlement.NetAmount);
            return settlement;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "AmazonTR GetSettlement exception for {StartDate}–{EndDate}", startDate, endDate);
            return null;
        }
    }

    /// <summary>
    /// Amazon TR cargo invoices — not available via SP-API; returns empty list.
    /// </summary>
    public Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(DateTime startDate, CancellationToken ct = default)
    {
        _logger.LogInformation("AmazonTR GetCargoInvoices: cargo invoice API not available via SP-API — returning empty list");
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
            var url = $"/orders/v0/orders?MarketplaceIds={TurkeyMarketplaceId}&OrderStatuses=Returned,Cancelled&MaxResultsPerPage=50";
            if (since.HasValue)
                url += "&CreatedAfter=" + since.Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            else
                url += "&CreatedAfter=" + DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

            var response = await ThrottledExecuteAsync(
                async token =>
                {
                    var req = await CreateAuthenticatedRequestAsync(HttpMethod.Get, url, token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("AmazonTR PullClaims failed: {Status} {Error}",
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
                            ? ccEl.GetString() ?? "TRY" : "TRY",
                        ClaimDate = order.TryGetProperty("PurchaseDate", out var pdEl) && pdEl.TryGetDateTime(out var pd) ? pd : DateTime.UtcNow
                    });
                }
            }

            _logger.LogInformation("AmazonTR PullClaims: {Count} claims fetched", claims.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AmazonTR PullClaims exception");
        }

        return claims;
    }

    /// <inheritdoc />
    public Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    {
        // Amazon auto-processes most returns — manual approval is not supported via SP-API.
        // Log and return true to indicate acknowledgement.
        _logger.LogInformation("AmazonTR ApproveClaimAsync: Amazon auto-processes returns. ClaimId={ClaimId} acknowledged", claimId);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    {
        // Amazon does not support claim rejection via SP-API — returns are auto-processed.
        _logger.LogWarning("AmazonTR RejectClaimAsync: Amazon does not support return rejection via API. ClaimId={ClaimId}, Reason={Reason}", claimId, reason);
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

            _logger.LogDebug("Amazon TR ping: {StatusCode}", response.StatusCode);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Amazon TR ping failed");
            return false;
        }
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
            "AmazonTrAdapter.SendInvoiceLinkAsync: Package={PackageId} — URL-only not natively supported by Amazon SP-API, skipping feed. InvoiceUrl={InvoiceUrl}",
            shipmentPackageId, invoiceUrl);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Submits PDF invoice via Amazon SP-API Feeds (UPLOAD_VAT_INVOICE feed type).
    /// Step 1: POST /feeds/2021-06-30/feeds to create feed document.
    /// Step 2: Upload PDF to the feed document URL.
    /// </remarks>
    public async Task<bool> SendInvoiceFileAsync(string shipmentPackageId, byte[] pdfBytes, string fileName, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("AmazonTrAdapter.SendInvoiceFileAsync: Package={PackageId} File={FileName}",
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
                _logger.LogError("AmazonTR CreateFeedDocument failed: {Status} - {Error}",
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
                _logger.LogError("AmazonTR CreateFeedDocument returned empty feedDocumentId or url");
                return false;
            }

            // Step 2: Upload PDF to the pre-signed URL
            using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
            uploadRequest.Content = new ByteArrayContent(pdfBytes);
            uploadRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

            var uploadResponse = await _httpClient.SendAsync(uploadRequest, ct).ConfigureAwait(false);
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var error = await uploadResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("AmazonTR UploadInvoicePdf failed: {Status} - {Error}",
                    uploadResponse.StatusCode, error);
                return false;
            }

            // Step 3: Create feed referencing the document
            var feedPayload = new
            {
                feedType = "UPLOAD_VAT_INVOICE",
                marketplaceIds = new[] { TurkeyMarketplaceId },
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
                _logger.LogError("AmazonTR CreateFeed (UPLOAD_VAT_INVOICE) failed: {Status} - {Error}",
                    feedResponse.StatusCode, error);
                return false;
            }

            _logger.LogInformation("AmazonTR SendInvoiceFile OK: Package={PackageId} FeedDocumentId={FeedDocumentId}",
                shipmentPackageId, feedDocumentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AmazonTR SendInvoiceFile exception: Package={PackageId}", shipmentPackageId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // IWebhookCapableAdapter — Amazon uses SQS/EventBridge, not HTTP webhooks
    // ═══════════════════════════════════════════

    /// <inheritdoc />
    /// <remarks>
    /// Amazon SP-API uses EventBridge / SQS for notifications instead of HTTP webhooks.
    /// Webhook registration must be configured in Seller Central or via Notifications API (destination + subscription).
    /// This method logs the guidance and returns true to indicate acknowledgement.
    /// </remarks>
    public Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AmazonTrAdapter.RegisterWebhookAsync: Amazon uses EventBridge/SQS notifications — configure in Seller Central or via Notifications SP-API. CallbackUrl={Url} ignored.",
            callbackUrl);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Amazon SP-API notification subscriptions are managed via Seller Central or Notifications API.
    /// This method logs and returns true.
    /// </remarks>
    public Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("AmazonTrAdapter.UnregisterWebhookAsync: Amazon notification subscriptions are managed via Seller Central — no HTTP unregister needed.");
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Processes an Amazon SNS notification JSON payload.
    /// Amazon EventBridge/SQS delivers notifications as SNS messages with a Topic and Message field.
    /// </remarks>
    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);

            var topic = doc.RootElement.TryGetProperty("TopicArn", out var topicEl) ? topicEl.GetString() :
                        doc.RootElement.TryGetProperty("notificationType", out var ntEl) ? ntEl.GetString() : "unknown";
            var messageId = doc.RootElement.TryGetProperty("MessageId", out var midEl) ? midEl.GetString() : null;

            _logger.LogInformation(
                "AmazonTrAdapter SNS webhook processed: Topic={Topic} MessageId={MessageId} PayloadLength={Length}",
                topic, messageId, payload.Length);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[AmazonTr] Malformed webhook payload ({Length}b)", payload?.Length ?? 0);
        }
        return Task.CompletedTask;
    }
}
