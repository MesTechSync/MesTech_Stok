using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Fulfillment;

/// <summary>
/// Amazon FBA (Fulfillment by Amazon) provider — SP-API entegrasyonu.
/// Inbound Eligibility API 2024-03-20 + FBA Inventory API v1.
/// LWA OAuth2 auth paylasilir (AmazonTrAdapter ile ayni token endpoint).
/// MarketplaceId: A33AVAJ2PDY3EV (Turkey)
/// </summary>
public sealed class AmazonFBAAdapter : IFulfillmentProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AmazonFBAAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    // LWA Auth state
    private string _refreshToken = string.Empty;
    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;
    private string _sellerId = string.Empty;
    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private const string TurkeyMarketplaceId = "A33AVAJ2PDY3EV";
    private const string SpApiBaseUrl = "https://sellingpartnerapi-eu.amazon.com";
    private const string LwaEndpoint = "https://api.amazon.com/auth/o2/token";

    public AmazonFBAAdapter(
        HttpClient httpClient,
        ILogger<AmazonFBAAdapter> logger,
        string refreshToken,
        string clientId,
        string clientSecret,
        string sellerId)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _refreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _sellerId = sellerId ?? throw new ArgumentNullException(nameof(sellerId));

        if (_httpClient.BaseAddress == null)
            _httpClient.BaseAddress = new Uri(SpApiBaseUrl, UriKind.Absolute);

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MesTech-AmazonFBA-Client/1.0");

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
                        "[AmazonFBA] SP-API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[AmazonFBA] Circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public FulfillmentCenter Center => FulfillmentCenter.AmazonFBA;

    // ═══════════════════════════════════════════
    // LWA OAuth2 Token Management
    // (SP-API: shared with AmazonTrAdapter pattern)
    // ═══════════════════════════════════════════

    private async Task EnsureFreshTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            return;

        using var lwaClient = new HttpClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = _refreshToken,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        });

        var response = await lwaClient.PostAsync(LwaEndpoint, content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
            cancellationToken: ct).ConfigureAwait(false);

        _accessToken = json.RootElement.GetProperty("access_token").GetString()!;
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);
    }

    private async Task<HttpRequestMessage> CreateAuthRequestAsync(
        HttpMethod method, string path, CancellationToken ct)
    {
        await EnsureFreshTokenAsync(ct).ConfigureAwait(false);
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("x-amz-access-token", _accessToken);
        return request;
    }

    // ═══════════════════════════════════════════
    // IFulfillmentProvider implementation
    // ═══════════════════════════════════════════

    /// <summary>
    /// Creates an FBA inbound plan via SP-API Inbound Eligibility v2024-03-20.
    /// POST /inbound/fba/2024-03-20/inboundPlans
    /// </summary>
    public async Task<InboundResult> CreateInboundShipmentAsync(
        InboundShipmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[AmazonFBA] CreateInboundShipment: {Name} — {ItemCount} items",
            request.ShipmentName, request.Items.Count);

        try
        {
            var payload = new
            {
                name = request.ShipmentName,
                sourceAddress = new
                {
                    name = _sellerId,
                    addressLine1 = "TBD",
                    city = "Istanbul",
                    stateOrProvinceCode = "Istanbul",
                    postalCode = "34000",
                    countryCode = "TR"
                },
                destinationMarketplaces = new[] { TurkeyMarketplaceId },
                items = request.Items.Select(i => new
                {
                    msku = i.SKU,
                    prepOwner = "SELLER",
                    labelOwner = "SELLER",
                    quantity = i.Quantity
                }).ToArray()
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(async token =>
            {
                var req = await CreateAuthRequestAsync(
                    HttpMethod.Post,
                    "/inbound/fba/2024-03-20/inboundPlans",
                    token).ConfigureAwait(false);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("[AmazonFBA] CreateInboundShipment failed: {Status} — {Error}",
                    response.StatusCode, error);
                return new InboundResult(false, string.Empty,
                    $"SP-API error {response.StatusCode}: {error}");
            }

            var respContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(respContent);

            var inboundPlanId = doc.RootElement.TryGetProperty("inboundPlanId", out var planId)
                ? planId.GetString() ?? Guid.NewGuid().ToString()
                : Guid.NewGuid().ToString();

            _logger.LogInformation("[AmazonFBA] Inbound plan created: {PlanId}", inboundPlanId);
            return new InboundResult(true, inboundPlanId);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "[AmazonFBA] Circuit breaker open — CreateInboundShipment skipped");
            return new InboundResult(false, string.Empty, "Service temporarily unavailable (circuit breaker open)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AmazonFBA] CreateInboundShipment exception");
            return new InboundResult(false, string.Empty, ex.Message);
        }
    }

    /// <summary>
    /// Queries FBA inventory for given SKUs.
    /// GET /fba/inventory/v1/summaries?sellerSkus=...&marketplaceIds=A33AVAJ2PDY3EV&granularityType=Marketplace&granularityId=A33AVAJ2PDY3EV
    /// </summary>
    public async Task<FulfillmentInventory> GetInventoryLevelsAsync(
        IReadOnlyList<string> skus, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(skus);
        _logger.LogInformation("[AmazonFBA] GetInventoryLevels: {SkuCount} SKUs", skus.Count);

        var stocks = new List<FulfillmentStock>();

        try
        {
            // SP-API allows up to 50 SKUs per request — process in batches
            const int batchSize = 50;
            var batches = skus.Chunk(batchSize).ToList();

            foreach (var batch in batches)
            {
                ct.ThrowIfCancellationRequested();

                var skuParam = string.Join(",", batch.Select(Uri.EscapeDataString));
                var path = $"/fba/inventory/v1/summaries?sellerSkus={skuParam}" +
                           $"&marketplaceIds={TurkeyMarketplaceId}" +
                           "&granularityType=Marketplace" +
                           $"&granularityId={TurkeyMarketplaceId}";

                var response = await _retryPipeline.ExecuteAsync(async token =>
                {
                    var req = await CreateAuthRequestAsync(HttpMethod.Get, path, token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("[AmazonFBA] GetInventory batch failed: {Status} — {Error}",
                        response.StatusCode, error);
                    continue;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("payload", out var payload) &&
                    payload.TryGetProperty("inventorySummaries", out var summaries))
                {
                    foreach (var item in summaries.EnumerateArray())
                    {
                        var sku = item.TryGetProperty("sellerSku", out var skuEl)
                            ? skuEl.GetString() ?? ""
                            : "";

                        var available = 0;
                        var reserved = 0;
                        var inbound = 0;

                        if (item.TryGetProperty("inventoryDetails", out var details))
                        {
                            if (details.TryGetProperty("fulfillableQuantity", out var fq))
                                available = fq.GetInt32();
                            if (details.TryGetProperty("reservedQuantity", out var rq))
                                reserved = rq.TryGetProperty("totalReservedQuantity", out var trq)
                                    ? trq.GetInt32() : 0;
                            if (details.TryGetProperty("inboundWorkingQuantity", out var iwq))
                                inbound = iwq.GetInt32();
                        }

                        if (!string.IsNullOrEmpty(sku))
                            stocks.Add(new FulfillmentStock(sku, available, reserved, inbound));
                    }
                }
            }

            _logger.LogInformation("[AmazonFBA] Inventory query complete: {Count} records", stocks.Count);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "[AmazonFBA] Circuit breaker open — GetInventoryLevels returning empty");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AmazonFBA] GetInventoryLevels exception");
        }

        return new FulfillmentInventory(Center, stocks.AsReadOnly(), DateTime.UtcNow);
    }

    /// <summary>
    /// Gets the operation status of an inbound plan.
    /// GET /inbound/fba/2024-03-20/inboundPlans/{inboundPlanId}/operationStatus
    /// </summary>
    public async Task<InboundStatus> GetInboundStatusAsync(
        string shipmentId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shipmentId, nameof(shipmentId));
        _logger.LogInformation("[AmazonFBA] GetInboundStatus: {ShipmentId}", shipmentId);

        try
        {
            var path = $"/inbound/fba/2024-03-20/inboundPlans/{Uri.EscapeDataString(shipmentId)}/operationStatus";

            var response = await _retryPipeline.ExecuteAsync(async token =>
            {
                var req = await CreateAuthRequestAsync(HttpMethod.Get, path, token).ConfigureAwait(false);
                return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("[AmazonFBA] GetInboundStatus failed: {Status} — {Error}",
                    response.StatusCode, error);
                return new InboundStatus(shipmentId, "UNKNOWN", 0, 0);
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var status = doc.RootElement.TryGetProperty("status", out var st)
                ? st.GetString() ?? "UNKNOWN"
                : "UNKNOWN";

            var totalExpected = 0;
            var totalReceived = 0;

            if (doc.RootElement.TryGetProperty("itemDetails", out var details))
            {
                if (details.TryGetProperty("totalExpectedQuantity", out var teq))
                    totalExpected = teq.GetInt32();
                if (details.TryGetProperty("totalReceivedQuantity", out var trq))
                    totalReceived = trq.GetInt32();
            }

            _logger.LogInformation("[AmazonFBA] Inbound status {ShipmentId}: {Status}", shipmentId, status);
            return new InboundStatus(shipmentId, status, totalExpected, totalReceived);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "[AmazonFBA] Circuit breaker open — GetInboundStatus unavailable");
            return new InboundStatus(shipmentId, "UNAVAILABLE", 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AmazonFBA] GetInboundStatus exception: {ShipmentId}", shipmentId);
            return new InboundStatus(shipmentId, "ERROR", 0, 0);
        }
    }

    /// <summary>
    /// Gets FBA fulfillment orders since the specified date.
    /// GET /fba/outbound/2020-07-01/fulfillmentOrders?queryStartDate={since}
    /// </summary>
    public async Task<IReadOnlyList<FulfillmentOrderResult>> GetFulfillmentOrdersAsync(
        DateTime since, CancellationToken ct = default)
    {
        _logger.LogInformation("[AmazonFBA] GetFulfillmentOrders since {Since}", since);

        var orders = new List<FulfillmentOrderResult>();

        try
        {
            var queryDate = since.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nextToken = (string?)null;

            do
            {
                ct.ThrowIfCancellationRequested();

                var path = nextToken is not null
                    ? $"/fba/outbound/2020-07-01/fulfillmentOrders?nextToken={Uri.EscapeDataString(nextToken)}"
                    : $"/fba/outbound/2020-07-01/fulfillmentOrders?queryStartDate={Uri.EscapeDataString(queryDate)}";

                var response = await _retryPipeline.ExecuteAsync(async token =>
                {
                    var req = await CreateAuthRequestAsync(HttpMethod.Get, path, token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("[AmazonFBA] GetFulfillmentOrders failed: {Status} — {Error}",
                        response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("payload", out var payload) &&
                    payload.TryGetProperty("fulfillmentOrders", out var fulfillmentOrders))
                {
                    foreach (var order in fulfillmentOrders.EnumerateArray())
                    {
                        var orderId = order.TryGetProperty("sellerFulfillmentOrderId", out var oid)
                            ? oid.GetString() ?? "" : "";
                        var status = order.TryGetProperty("fulfillmentOrderStatus", out var st)
                            ? st.GetString() ?? "UNKNOWN" : "UNKNOWN";

                        DateTime? shippedDate = null;
                        if (order.TryGetProperty("statusUpdatedDate", out var sud) &&
                            DateTime.TryParse(sud.GetString(), out var parsedShipped))
                            shippedDate = parsedShipped;

                        var items = new List<FulfillmentOrderItem>();
                        if (order.TryGetProperty("fulfillmentOrderItems", out var orderItems))
                        {
                            foreach (var item in orderItems.EnumerateArray())
                            {
                                var sku = item.TryGetProperty("sellerSku", out var skuEl)
                                    ? skuEl.GetString() ?? "" : "";
                                var qtyOrdered = item.TryGetProperty("quantity", out var qo)
                                    ? qo.GetInt32() : 0;
                                var qtyShipped = item.TryGetProperty("unfulfillableQuantity", out var uq)
                                    ? qtyOrdered - uq.GetInt32() : qtyOrdered;

                                if (!string.IsNullOrEmpty(sku))
                                    items.Add(new FulfillmentOrderItem(sku, qtyOrdered, qtyShipped));
                            }
                        }

                        if (!string.IsNullOrEmpty(orderId))
                            orders.Add(new FulfillmentOrderResult(orderId, status, items.AsReadOnly(), shippedDate));
                    }
                }

                nextToken = payload.TryGetProperty("nextToken", out var nt)
                    ? nt.GetString() : null;

            } while (!string.IsNullOrEmpty(nextToken));

            _logger.LogInformation("[AmazonFBA] GetFulfillmentOrders complete: {Count} orders", orders.Count);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "[AmazonFBA] Circuit breaker open — GetFulfillmentOrders returning empty");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AmazonFBA] GetFulfillmentOrders exception");
        }

        return orders.AsReadOnly();
    }

    /// <summary>
    /// Health check: verifies SP-API access by calling FBA inventory endpoint with empty SKU list.
    /// </summary>
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            await EnsureFreshTokenAsync(ct).ConfigureAwait(false);

            // Lightweight check: query inventory with minimal params
            var path = $"/fba/inventory/v1/summaries?marketplaceIds={TurkeyMarketplaceId}" +
                       "&granularityType=Marketplace" +
                       $"&granularityId={TurkeyMarketplaceId}&details=false";

            var response = await _retryPipeline.ExecuteAsync(async token =>
            {
                var req = await CreateAuthRequestAsync(HttpMethod.Get, path, token).ConfigureAwait(false);
                return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            var available = response.IsSuccessStatusCode || (int)response.StatusCode < 500;
            _logger.LogInformation("[AmazonFBA] IsAvailable: {Available} ({Status})", available, response.StatusCode);
            return available;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AmazonFBA] IsAvailable check failed");
            return false;
        }
    }
}
