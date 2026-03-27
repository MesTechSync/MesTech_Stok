using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Auth;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Pazarama platform adaptoru — Dalga 4 tam entegrasyon.
/// IIntegratorAdapter + IOrderCapableAdapter + IShipmentCapableAdapter +
/// IClaimCapableAdapter + IInvoiceCapableAdapter + IWebhookCapableAdapter.
/// OAuth 2.0 Client Credentials auth, Polly retry, SemaphoreSlim rate limiting.
/// Async batch product create with polling.
/// 2-stage cargo notification (Hazirlaniyor → Kargoya Verildi).
/// </summary>
public sealed class PazaramaAdapter : IIntegratorAdapter, IOrderCapableAdapter,
    IShipmentCapableAdapter, IClaimCapableAdapter, IInvoiceCapableAdapter,
    ISettlementCapableAdapter, IWebhookCapableAdapter, IPingableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<PazaramaAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);

    private OAuth2AuthProvider? _authProvider;
    private bool _isConfigured;

    public PazaramaAdapter(HttpClient httpClient, ILogger<PazaramaAdapter> logger, IHttpClientFactory? httpClientFactory = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClientFactory = httpClientFactory;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            // HTTP 429 rate-limit retry — Pazarama API throttling
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
                    _logger.LogWarning("Pazarama rate limited (429). Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("Pazarama API retry {Attempt} after {Delay}ms",
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

    public string PlatformCode => nameof(PlatformType.Pazarama);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ── Auth (OAuth 2.0 Client Credentials) ──────────────
    private void ConfigureAuth(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var clientId = credentials.GetValueOrDefault("PazaramaClientId", "");
        var clientSecret = credentials.GetValueOrDefault("PazaramaClientSecret", "");

        // BaseUrl override for WireMock testing
        var baseUrl = credentials.GetValueOrDefault("BaseUrl", "");
        if (!string.IsNullOrEmpty(baseUrl))
            _httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

        // Determine token endpoint — use WireMock URL when BaseUrl provided
        var tokenEndpoint = !string.IsNullOrEmpty(baseUrl)
            ? $"{baseUrl.TrimEnd('/')}/connect/token"
            : "https://isortagimgiris.pazarama.com/connect/token";

        var tokenHttpClient = _httpClientFactory?.CreateClient("PazaramaToken")
            ?? throw new InvalidOperationException("IHttpClientFactory is required for PazaramaToken client creation");

        var loggerFactory = new LoggerFactory();

        _authProvider = new OAuth2AuthProvider(
            platformCode: "Pazarama",
            httpClient: tokenHttpClient,
            tokenCache: new InMemoryTokenCacheProvider(),
            clientId: clientId,
            clientSecret: clientSecret,
            tokenEndpoint: tokenEndpoint,
            scope: "merchantgatewayapi.fullaccess",
            logger: loggerFactory.CreateLogger<OAuth2AuthProvider>());

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MesTech-Pazarama-Client/4.0");

        _isConfigured = true;
    }

    private async Task EnsureAuthHeaderAsync(CancellationToken ct)
    {
        if (_authProvider is null)
            throw new InvalidOperationException(
                "PazaramaAdapter henuz konfigure edilmedi. Once TestConnectionAsync cagirin.");

        var token = await _authProvider.GetTokenAsync(ct).ConfigureAwait(false);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "PazaramaAdapter henuz konfigure edilmedi. Once TestConnectionAsync cagirin.");
    }

    // ── TestConnection ──────────────────────────────────
    public async Task<ConnectionTestResultDto> TestConnectionAsync(
        Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var sw = Stopwatch.StartNew();
        var result = new ConnectionTestResultDto { PlatformCode = PlatformCode };

        try
        {
            if (!credentials.ContainsKey("PazaramaClientId") ||
                string.IsNullOrWhiteSpace(credentials["PazaramaClientId"]))
            {
                result.ErrorMessage = "PazaramaClientId alani zorunludur.";
                result.ResponseTime = sw.Elapsed;
                return result;
            }

            if (!credentials.ContainsKey("PazaramaClientSecret") ||
                string.IsNullOrWhiteSpace(credentials["PazaramaClientSecret"]))
            {
                result.ErrorMessage = "PazaramaClientSecret alani zorunludur.";
                result.ResponseTime = sw.Elapsed;
                return result;
            }

            ConfigureAuth(credentials);
            await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, "/brand/getBrands?Page=1&Size=1"), ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;
            sw.Stop();
            result.ResponseTime = sw.Elapsed;

            if (response.IsSuccessStatusCode)
            {
                result.IsSuccess = true;
                result.StoreName = "Pazarama Magaza";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Yetkisiz erisim — OAuth2 token hatali.",
                    System.Net.HttpStatusCode.Forbidden => "Erisim engellendi — Yetkisiz istemci.",
                    _ => $"Pazarama API hatasi: {response.StatusCode} - {errorContent}"
                };
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            result.ErrorMessage = $"Baglanti hatasi: {ex.Message}";
            result.ResponseTime = sw.Elapsed;
            _logger.LogError(ex, "Pazarama TestConnection failed");
        }

        return result;
    }

    // ── Product methods ─────────────────────────────────

    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(product);
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        var createRequest = new PzProductCreateRequest
        {
            Products = new List<PzProductDetail>
            {
                new PzProductDetail
                {
                    Code = product.SKU,
                    Name = product.Name,
                    DisplayName = product.Name,
                    SalePrice = product.SalePrice,
                    ListPrice = product.SalePrice,
                    StockCount = product.Stock,
                    Description = product.Description,
                    Barcode = product.Barcode,
                    State = 3 // Active
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest, _jsonOptions);
        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/product/create");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Pazarama PushProduct failed {Status}: {Error}", response.StatusCode, error);
            return false;
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var createResponse = JsonSerializer.Deserialize<PzApiResponse<PzProductCreateResponse>>(content, _jsonOptions);

        if (createResponse?.Data is null || createResponse.Data.BatchRequestId == Guid.Empty)
        {
            _logger.LogWarning("Pazarama PushProduct — no batchRequestId returned");
            return false;
        }

        // Poll for batch result
        return await PollBatchResultAsync(createResponse.Data.BatchRequestId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Polls batch result endpoint until done, error, or timeout (30s, 2s interval).
    /// </summary>
    private async Task<bool> PollBatchResultAsync(Guid batchRequestId, CancellationToken ct)
    {
        var timeout = TimeSpan.FromSeconds(30);
        var interval = TimeSpan.FromSeconds(2);
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            await Task.Delay(interval, ct).ConfigureAwait(false);

            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get,
                    $"/product/getProductBatchResult?BatchRequestId={batchRequestId}"), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Pazarama batch poll failed {Status}: {Error}", response.StatusCode, errorBody);
                continue;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var batchResult = JsonSerializer.Deserialize<PzApiResponse<PzBatchResultResponse>>(content, _jsonOptions);

            if (batchResult?.Data is null)
                continue;

            switch (batchResult.Data.Status)
            {
                case 2: // Done
                    if (batchResult.Data.FailedCount > 0)
                    {
                        _logger.LogWarning("Pazarama batch {BatchId} done with {Failed} failures",
                            batchRequestId, batchResult.Data.FailedCount);
                        return false;
                    }
                    _logger.LogInformation("Pazarama PushProduct batch OK: {BatchId}", batchRequestId);
                    return true;

                case 3: // Error
                    _logger.LogWarning("Pazarama batch {BatchId} returned error status", batchRequestId);
                    return false;

                default: // 1 = InProgress — continue polling
                    break;
            }
        }

        // Timeout — log warning, return true (batch may complete in background)
        _logger.LogWarning("Pazarama batch {BatchId} polling timed out after {Seconds}s — assuming background completion",
            batchRequestId, timeout.TotalSeconds);
        return true;
    }

    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        var products = new List<Product>();
        var page = 1;
        const int pageSize = 50;

        while (!ct.IsCancellationRequested)
        {
            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get,
                    $"/product/products?Approved=true&Page={page}&Size={pageSize}"), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Pazarama PullProducts page {Page} failed: {Status} {Error}",
                    page, response.StatusCode, errorBody);
                break;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var pzResponse = JsonSerializer.Deserialize<PzProductListResponse>(content, _jsonOptions);

            if (pzResponse?.Data is null || pzResponse.Data.Count == 0)
                break;

            foreach (var pzProduct in pzResponse.Data)
            {
                products.Add(new Product
                {
                    Name = pzProduct.Name,
                    SKU = pzProduct.Code,
                    Barcode = null,
                    Description = null,
                    SalePrice = pzProduct.SalePrice,
                    Stock = pzProduct.StockCount,
                    IsActive = pzProduct.State == 3 // State 3 = Active
                });
            }

            page++;

            // No totalCount in response — stop when we get fewer than pageSize
            if (pzResponse.Data.Count < pageSize)
                break;
        }

        _logger.LogInformation("Pazarama PullProducts: {Count} products fetched", products.Count);
        return products.AsReadOnly();
    }

    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        var payload = new PzStockUpdateRequest
        {
            Items = new List<PzStockItem>
            {
                new PzStockItem { Code = productId.ToString(), StockCount = newStock }
            }
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/product/updateStock");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Pazarama stock update OK: {ProductId} -> {Stock}", productId, newStock);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Pazarama stock update failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        var payload = new PzPriceUpdateRequest
        {
            Items = new List<PzPriceItem>
            {
                new PzPriceItem
                {
                    Code = productId.ToString(),
                    ListPrice = newPrice,
                    SalePrice = newPrice
                }
            }
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/product/updatePrice");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Pazarama price update OK: {ProductId} -> {Price}", productId, newPrice);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Pazarama price update failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Order methods (IOrderCapableAdapter) ─────────────

    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        var orders = new List<ExternalOrderDto>();
        var page = 1;
        const int pageSize = 50;

        // Default last 7 days if since is null
        var startDate = since ?? DateTime.UtcNow.AddDays(-7);

        // Cap range at 1 month
        var endDate = DateTime.UtcNow.AddDays(1); // exclusive
        if ((endDate - startDate).TotalDays > 31)
            startDate = endDate.AddDays(-31);

        while (!ct.IsCancellationRequested)
        {
            var requestBody = new PzOrderListRequest
            {
                PageSize = pageSize,
                PageNumber = page,
                StartDate = startDate,
                EndDate = endDate
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);

            var response = await ExecuteWithRetryAsync(
                () =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, "/order/getOrdersForApi");
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    return req;
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Pazarama PullOrders page {Page} failed: {Status} {Error}",
                    page, response.StatusCode, errorBody);
                break;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var pzResponse = JsonSerializer.Deserialize<PzOrderListResponse>(content, _jsonOptions);

            if (pzResponse?.Data is null || pzResponse.Data.Count == 0)
                break;

            foreach (var pzOrder in pzResponse.Data)
            {
                // Map each order item to individual ExternalOrderDto entry
                foreach (var item in pzOrder.Items)
                {
                    orders.Add(new ExternalOrderDto
                    {
                        PlatformOrderId = $"{pzOrder.OrderNumber}:{item.OrderItemId}",
                        PlatformCode = PlatformCode,
                        OrderNumber = pzOrder.OrderNumber.ToString(),
                        Status = MapOrderStatus(item.OrderItemStatus),
                        CustomerName = pzOrder.CustomerName,
                        CustomerAddress = pzOrder.ShipmentAddress?.Address,
                        CustomerCity = pzOrder.ShipmentAddress?.City,
                        CustomerPhone = pzOrder.ShipmentAddress?.Phone,
                        TotalAmount = item.TotalPrice,
                        CargoProviderName = item.Cargo?.CompanyName,
                        CargoTrackingNumber = item.Cargo?.TrackingNumber,
                        ShipmentPackageId = item.ShipmentCode ?? pzOrder.OrderId.ToString(),
                        OrderDate = pzOrder.OrderDate,
                        Lines = new List<ExternalOrderLineDto>
                        {
                            new ExternalOrderLineDto
                            {
                                PlatformLineId = item.OrderItemId.ToString(),
                                SKU = item.Product?.Code,
                                ProductName = item.Product?.Name ?? "",
                                Quantity = item.Quantity,
                                UnitPrice = item.SalePrice,
                                TaxRate = item.Product?.VatRate ?? 0,
                                LineTotal = item.TotalPrice
                            }
                        }
                    });
                }
            }

            page++;

            // Stop when fewer than pageSize results
            if (pzResponse.Data.Count < pageSize)
                break;
        }

        _logger.LogInformation("Pazarama PullOrders: {Count} order items fetched", orders.Count);
        return orders.AsReadOnly();
    }

    public async Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        // packageId format: "orderNumber:orderItemId"
        var parts = packageId.Split(':');
        if (parts.Length != 2 || !long.TryParse(parts[0], out var orderNumber) ||
            !Guid.TryParse(parts[1], out var orderItemId))
        {
            _logger.LogWarning("Pazarama UpdateOrderStatus — invalid packageId format: {PackageId}", packageId);
            return false;
        }

        var statusCode = status switch
        {
            "Hazirlaniyor" => 12,
            "KargoyaVerildi" => 5,
            "Teslim Edildi" => 6,
            "Iptal" => 7,
            _ => int.TryParse(status, out var s) ? s : 0
        };

        var payload = new PzUpdateOrderStatusRequest
        {
            OrderNumber = orderNumber,
            Item = new PzOrderStatusItem
            {
                OrderItemId = orderItemId,
                Status = statusCode
            }
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "/order/updateOrderStatus");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Pazarama order status updated: {PackageId} -> {Status}", packageId, status);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Pazarama order status update failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Shipment (IShipmentCapableAdapter) — 2-Stage ─────

    public async Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber,
        CargoProvider provider, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        // platformOrderId format: "orderNumber:orderItemId"
        var parts = platformOrderId.Split(':');
        if (parts.Length != 2 || !long.TryParse(parts[0], out var orderNumber) ||
            !Guid.TryParse(parts[1], out var orderItemId))
        {
            _logger.LogWarning("Pazarama SendShipment — invalid platformOrderId format: {Id}", platformOrderId);
            return false;
        }

        // Stage 1: status=12 (Hazirlaniyor)
        var stage1Payload = new PzUpdateOrderStatusRequest
        {
            OrderNumber = orderNumber,
            Item = new PzOrderStatusItem
            {
                OrderItemId = orderItemId,
                Status = 12, // Hazirlaniyor
                DeliveryType = 1
            }
        };
        var stage1Json = JsonSerializer.Serialize(stage1Payload, _jsonOptions);

        var stage1Response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "/order/updateOrderStatus");
                req.Content = new StringContent(stage1Json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (!stage1Response.IsSuccessStatusCode)
        {
            var error = await stage1Response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Pazarama shipment stage 1 (Hazirlaniyor) failed {Status}: {Error}",
                stage1Response.StatusCode, error);
            return false;
        }

        _logger.LogInformation("Pazarama shipment stage 1 OK: {OrderId} -> Hazirlaniyor", platformOrderId);

        // Stage 2: status=5 (Kargoya Verildi) + tracking number
        var stage2Payload = new PzUpdateOrderStatusRequest
        {
            OrderNumber = orderNumber,
            Item = new PzOrderStatusItem
            {
                OrderItemId = orderItemId,
                Status = 5, // Kargoya Verildi
                DeliveryType = 1,
                ShippingTrackingNumber = trackingNumber
            }
        };
        var stage2Json = JsonSerializer.Serialize(stage2Payload, _jsonOptions);

        var stage2Response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "/order/updateOrderStatus");
                req.Content = new StringContent(stage2Json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (stage2Response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Pazarama shipment stage 2 OK: {OrderId} -> Kargoya Verildi, Tracking={Tracking}",
                platformOrderId, trackingNumber);
            return true;
        }

        var stage2Error = await stage2Response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Pazarama shipment stage 2 (Kargoya Verildi) failed {Status}: {Error}",
            stage2Response.StatusCode, stage2Error);
        return false;
    }

    // ── Claims (IClaimCapableAdapter) ────────────────────

    public async Task<IReadOnlyList<ExternalClaimDto>> PullClaimsAsync(
        DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        var claims = new List<ExternalClaimDto>();
        var page = 1;
        const int pageSize = 50;

        while (!ct.IsCancellationRequested)
        {
            var requestBody = new PzRefundListRequest
            {
                PageSize = pageSize,
                PageNumber = page,
                RefundStatus = 1, // Onay Bekliyor (Pending Approval)
                RequestStartDate = since,
                RequestEndDate = since.HasValue ? DateTime.UtcNow.AddDays(1) : null
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);

            var response = await ExecuteWithRetryAsync(
                () =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, "/order/getRefund");
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    return req;
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Pazarama PullClaims page {Page} failed: {Status} {Error}",
                    page, response.StatusCode, errorBody);
                break;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var pzResponse = JsonSerializer.Deserialize<PzRefundListResponse>(content, _jsonOptions);

            if (pzResponse?.Data?.RefundList is null || pzResponse.Data.RefundList.Count == 0)
                break;

            foreach (var refund in pzResponse.Data.RefundList)
            {
                claims.Add(new ExternalClaimDto
                {
                    PlatformClaimId = refund.RefundId.ToString(),
                    PlatformCode = PlatformCode,
                    OrderNumber = refund.OrderNumber.ToString(),
                    Status = MapRefundStatus(refund.RefundStatus),
                    Reason = refund.RefundType switch
                    {
                        1 => "Iade",
                        2 => "Iptal",
                        _ => "Diger"
                    },
                    CustomerName = refund.CustomerName,
                    Amount = refund.RefundAmount,
                    ClaimDate = DateTime.UtcNow, // Pazarama does not return claim date in list
                    Lines = new List<ExternalClaimLineDto>
                    {
                        new ExternalClaimLineDto
                        {
                            SKU = refund.ProductCode,
                            ProductName = refund.ProductName,
                            Quantity = 1,
                            UnitPrice = refund.RefundAmount
                        }
                    }
                });
            }

            page++;

            // Stop when fewer than pageSize
            if (pzResponse.Data.RefundList.Count < pageSize)
                break;
        }

        _logger.LogInformation("Pazarama PullClaims: {Count} claims fetched", claims.Count);
        return claims.AsReadOnly();
    }

    public async Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        if (!Guid.TryParse(claimId, out var refundId))
        {
            _logger.LogWarning("Pazarama ApproveClaim — invalid claimId format: {ClaimId}", claimId);
            return false;
        }

        var payload = new PzUpdateRefundRequest
        {
            RefundId = refundId,
            Status = 2 // Onay (Approved)
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/order/updateRefund");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Pazarama claim approved: {ClaimId}", claimId);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Pazarama claim approval failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    public async Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        if (!Guid.TryParse(claimId, out var refundId))
        {
            _logger.LogWarning("Pazarama RejectClaim — invalid claimId format: {ClaimId}", claimId);
            return false;
        }

        // Note: Pazarama API does not accept reason text, only status code
        var payload = new PzUpdateRefundRequest
        {
            RefundId = refundId,
            Status = 3 // Ret (Rejected)
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/order/updateRefund");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Pazarama claim rejected: {ClaimId} (reason not sent to API: {Reason})",
                claimId, reason);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Pazarama claim rejection failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Invoice (IInvoiceCapableAdapter) ─────────────────

    public async Task<bool> SendInvoiceLinkAsync(string shipmentPackageId, string invoiceUrl,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        if (!Guid.TryParse(shipmentPackageId, out var orderId))
        {
            _logger.LogWarning("Pazarama SendInvoiceLink — invalid shipmentPackageId format: {Id}",
                shipmentPackageId);
            return false;
        }

        var payload = new PzInvoiceLinkRequest
        {
            InvoiceLink = invoiceUrl,
            OrderId = orderId
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/order/invoice-link");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Pazarama invoice link sent: OrderId={OrderId} Url={Url}",
                shipmentPackageId, invoiceUrl);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Pazarama invoice link failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    public Task<bool> SendInvoiceFileAsync(string shipmentPackageId, byte[] pdfBytes,
        string fileName, CancellationToken ct = default)
    {
        // Pazarama only accepts invoice URL, not file upload
        _logger.LogWarning("Pazarama SendInvoiceFile not supported — use SendInvoiceLinkAsync instead");
        return Task.FromResult(false);
    }

    // ── Categories ──────────────────────────────────────

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/category/getCategoryTree"), ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Pazarama GetCategories failed: {Status} {Error}", response.StatusCode, errorBody);
            return Array.Empty<CategoryDto>();
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var pzResponse = JsonSerializer.Deserialize<PzApiResponse<List<PzCategoryTree>>>(content, _jsonOptions);

        if (pzResponse?.Data is null)
            return Array.Empty<CategoryDto>();

        // Filter leaf categories only
        var leafCategories = new List<CategoryDto>();
        CollectLeafCategories(pzResponse.Data, leafCategories);

        _logger.LogInformation("Pazarama GetCategories: {Count} leaf categories", leafCategories.Count);
        return leafCategories.AsReadOnly();
    }

    private static void CollectLeafCategories(List<PzCategoryTree> nodes, List<CategoryDto> result)
    {
        foreach (var node in nodes)
        {
            if (node.Leaf)
            {
                result.Add(new CategoryDto
                {
                    PlatformCategoryId = node.Id.GetHashCode(), // Guid to int mapping
                    Name = node.DisplayName
                });
            }

            if (node.ParentCategories.Count > 0)
                CollectLeafCategories(node.ParentCategories, result);
        }
    }

    // ── Status mappers ──────────────────────────────────

    private static string MapOrderStatus(int status) => status switch
    {
        1 => "Yeni",
        2 => "Onaylandi",
        3 => "Hazirlaniyor",
        5 => "Kargoya Verildi",
        6 => "Teslim Edildi",
        7 => "Iptal Edildi",
        12 => "Hazirlaniyor",
        _ => $"Bilinmeyen ({status})"
    };

    private static string MapRefundStatus(int status) => status switch
    {
        1 => "Onay Bekliyor",
        2 => "Onaylandi",
        3 => "Reddedildi",
        _ => $"Bilinmeyen ({status})"
    };

    // ── HTTP helper ─────────────────────────────────────

    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _retryPipeline.ExecuteAsync(async token =>
            {
                using var request = requestFactory();
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "{Platform} circuit breaker is open — returning 503", PlatformCode);
            return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Circuit breaker open")
            };
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    // ═══════════════════════════════════════════
    // ISettlementCapableAdapter
    // ═══════════════════════════════════════════

    /// <inheritdoc />
    public async Task<SettlementDto?> GetSettlementAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PazaramaAdapter.GetSettlementAsync: {StartDate} — {EndDate}", startDate, endDate);

        try
        {
            var url = $"/api/settlement?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

            var response = await ExecuteWithRetryAsync(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Relative));
                return req;
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Pazarama GetSettlement failed: {Status} - {Error}", response.StatusCode, error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var settlement = new SettlementDto
            {
                PlatformCode = "Pazarama",
                StartDate = startDate,
                EndDate = endDate,
                Currency = "TRY"
            };

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                settlement.TotalSales = data.TryGetProperty("totalSales", out var ts) && ts.TryGetDecimal(out var tsv) ? tsv : 0m;
                settlement.TotalCommission = data.TryGetProperty("totalCommission", out var tc) && tc.TryGetDecimal(out var tcv) ? tcv : 0m;
                settlement.TotalShippingCost = data.TryGetProperty("totalShippingCost", out var tsc) && tsc.TryGetDecimal(out var tscv) ? tscv : 0m;
                settlement.TotalReturnDeduction = data.TryGetProperty("totalReturnDeduction", out var trd) && trd.TryGetDecimal(out var trdv) ? trdv : 0m;
                settlement.NetAmount = data.TryGetProperty("netAmount", out var na) && na.TryGetDecimal(out var nav) ? nav : 0m;

                if (data.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array)
                {
                    foreach (var line in lines.EnumerateArray())
                    {
                        settlement.Lines.Add(new SettlementLineDto
                        {
                            OrderNumber = line.TryGetProperty("orderNumber", out var on) ? on.GetString() : null,
                            TransactionType = line.TryGetProperty("transactionType", out var tt) ? tt.GetString() : null,
                            Amount = line.TryGetProperty("amount", out var amt) && amt.TryGetDecimal(out var amtv) ? amtv : 0m,
                            CommissionAmount = line.TryGetProperty("commissionAmount", out var ca) && ca.TryGetDecimal(out var cav) ? cav : null,
                            TransactionDate = line.TryGetProperty("transactionDate", out var td) && DateTime.TryParse(td.GetString(), out var tdv) ? tdv : startDate
                        });
                    }
                }
            }

            _logger.LogInformation("Pazarama GetSettlement: {LineCount} lines, Net={Net} TRY",
                settlement.Lines.Count, settlement.NetAmount);

            return settlement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pazarama GetSettlement exception: {StartDate}—{EndDate}", startDate, endDate);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(DateTime startDate, CancellationToken ct = default)
    {
        _logger.LogInformation("PazaramaAdapter.GetCargoInvoicesAsync: Pazarama does not provide cargo invoices separately — returning empty list. StartDate={StartDate}", startDate);
        return Task.FromResult<IReadOnlyList<CargoInvoiceDto>>(Array.Empty<CargoInvoiceDto>());
    }

    // ═══════════════════════════════════════════
    // IWebhookCapableAdapter
    // ═══════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PazaramaAdapter.RegisterWebhookAsync: {Url}", callbackUrl);

        try
        {
            var payload = new
            {
                callbackUrl,
                events = new[] { "ORDER_CREATED", "ORDER_UPDATED", "RETURN_CREATED", "STOCK_ALERT" }
            };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await ExecuteWithRetryAsync(
                () =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/register");
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    return req;
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Pazarama RegisterWebhook failed: {Status} {Error}",
                    response.StatusCode, error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pazarama RegisterWebhook exception");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("PazaramaAdapter.UnregisterWebhookAsync");

        try
        {
            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Delete, "/api/webhook/unregister"), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Pazarama UnregisterWebhook failed: {Status} {Error}",
                    response.StatusCode, error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pazarama UnregisterWebhook exception");
            return false;
        }
    }

    /// <inheritdoc />
    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var eventType = doc.RootElement.TryGetProperty("eventType", out var et) ? et.GetString() : "unknown";

            _logger.LogInformation(
                "PazaramaAdapter webhook processed: EventType={EventType} PayloadLength={Length}",
                eventType, payload.Length);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[Pazarama] Malformed webhook payload ({Length}b)", payload?.Length ?? 0);
        }
        return Task.CompletedTask;
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
        catch (Exception ex) { _logger.LogWarning(ex, "Pazarama ping failed"); return false; }
    }
}
