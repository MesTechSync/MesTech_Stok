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
/// Hepsilojistik (Hepsiburada Fulfillment) provider.
/// Basic Auth: Base64(MerchantId:ApiKey) — shares HepsiburadaAdapter auth pattern.
/// API base: https://mpop.hepsiburada.com/lojistik/api/v1  (REST/JSON)
/// Note: Endpoint URLs, payload schemas, and response field names are provisional —
/// confirm with Hepsilojistik developer portal documentation when access is available.
/// </summary>
public sealed class HepsilojistikAdapter : IFulfillmentProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HepsilojistikAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);

    // Provisional base URL — confirm with official Hepsilojistik documentation
    private const string HepsilojistikBaseUrl = "https://lojistik-api.hepsiburada.com/v1";

    public HepsilojistikAdapter(
        HttpClient httpClient,
        ILogger<HepsilojistikAdapter> logger,
        string merchantId,
        string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantId, nameof(merchantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));

        if (_httpClient.BaseAddress == null)
            _httpClient.BaseAddress = new Uri(HepsilojistikBaseUrl, UriKind.Absolute);

        // Basic Auth: MerchantId:ApiKey (same pattern as HepsiburadaAdapter)
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{merchantId}:{apiKey}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent", "MesTech-Hepsilojistik-Client/1.0");

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
                        "[Hepsilojistik] API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[Hepsilojistik] Circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public FulfillmentCenter Center => FulfillmentCenter.Hepsilojistik;

    // ═══════════════════════════════════════════
    // IFulfillmentProvider implementation
    // ═══════════════════════════════════════════

    /// <summary>
    /// Sends products to Hepsilojistik warehouse for inbound receiving (POST /shipments).
    /// </summary>
    public async Task<InboundResult> CreateInboundShipmentAsync(
        InboundShipmentRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogInformation("[Hepsilojistik] CreateInboundShipment: {Name} — {ItemCount} items",
            request.ShipmentName, request.Items.Count);

        try
        {
            var payload = new
            {
                shipmentName = request.ShipmentName,
                expectedArrivalDate = request.ExpectedArrival?.ToString("yyyy-MM-dd"),
                notes = request.Notes,
                items = request.Items.Select(i => new
                {
                    merchantSku = i.SKU,
                    quantity = i.Quantity,
                    lotNumber = i.LotNumber,
                    expiryDate = i.ExpiryDate?.ToString("yyyy-MM-dd")
                }).ToArray()
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await ExecuteWithRetryAsync(
                () =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, "/shipments");
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    return req;
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("[Hepsilojistik] CreateInboundShipment failed: {Status} — {Error}",
                    response.StatusCode, error);
                return new InboundResult(false, string.Empty,
                    $"Hepsilojistik API error {response.StatusCode}: {error}");
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var shipmentId = doc.RootElement.TryGetProperty("shipmentId", out var sid)
                ? sid.GetString() ?? Guid.NewGuid().ToString()
                : doc.RootElement.TryGetProperty("id", out var id)
                    ? id.GetString() ?? Guid.NewGuid().ToString()
                    : Guid.NewGuid().ToString();

            _logger.LogInformation("[Hepsilojistik] Inbound shipment created: {ShipmentId}", shipmentId);
            return new InboundResult(true, shipmentId);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "[Hepsilojistik] Circuit breaker open — CreateInboundShipment skipped");
            return new InboundResult(false, string.Empty, "Service temporarily unavailable (circuit breaker open)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Hepsilojistik] CreateInboundShipment exception");
            return new InboundResult(false, string.Empty, ex.Message);
        }
    }

    /// <summary>
    /// Queries fulfillment stock levels for given SKUs (GET /inventory?merchantSkus=...).
    /// </summary>
    public async Task<FulfillmentInventory> GetInventoryLevelsAsync(
        IReadOnlyList<string> skus, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(skus);
        _logger.LogInformation("[Hepsilojistik] GetInventoryLevels: {SkuCount} SKUs", skus.Count);

        var stocks = new List<FulfillmentStock>();

        try
        {
            const int batchSize = 50;
            var batches = skus.Chunk(batchSize).ToList();

            foreach (var batch in batches)
            {
                ct.ThrowIfCancellationRequested();

                var skuParam = string.Join(",", batch.Select(Uri.EscapeDataString));
                var path = $"/inventory?merchantSkus={skuParam}";

                var response = await ExecuteWithRetryAsync(
                    () => new HttpRequestMessage(HttpMethod.Get, path), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("[Hepsilojistik] GetInventory batch failed: {Status} — {Error}",
                        response.StatusCode, error);
                    continue;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                var root = doc.RootElement;
                var itemsEl = root.ValueKind == JsonValueKind.Array
                    ? root
                    : root.TryGetProperty("items", out var items) ? items
                    : root.TryGetProperty("data", out var data) ? data
                    : default;

                if (itemsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in itemsEl.EnumerateArray())
                    {
                        var sku = item.TryGetProperty("merchantSku", out var skuEl)
                            ? skuEl.GetString() ?? ""
                            : item.TryGetProperty("sku", out var sku2) ? sku2.GetString() ?? "" : "";

                        var available = item.TryGetProperty("availableQuantity", out var aq) ? aq.GetInt32()
                            : item.TryGetProperty("available", out var av) ? av.GetInt32() : 0;
                        var reserved = item.TryGetProperty("reservedQuantity", out var rq) ? rq.GetInt32() : 0;
                        var inbound = item.TryGetProperty("inboundQuantity", out var iq) ? iq.GetInt32() : 0;

                        if (!string.IsNullOrEmpty(sku))
                            stocks.Add(new FulfillmentStock(sku, available, reserved, inbound));
                    }
                }
            }

            _logger.LogInformation("[Hepsilojistik] Inventory query complete: {Count} records", stocks.Count);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "[Hepsilojistik] Circuit breaker open — GetInventoryLevels returning empty");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Hepsilojistik] GetInventoryLevels exception");
        }

        return new FulfillmentInventory(Center, stocks.AsReadOnly(), DateTime.UtcNow);
    }

    /// <summary>
    /// Checks shipment receipt status at Hepsilojistik warehouse (GET /shipments/{id}/status).
    /// </summary>
    public async Task<InboundStatus> GetInboundStatusAsync(
        string shipmentId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shipmentId, nameof(shipmentId));
        _logger.LogInformation("[Hepsilojistik] GetInboundStatus: {ShipmentId}", shipmentId);

        try
        {
            var path = $"/shipments/{Uri.EscapeDataString(shipmentId)}/status";

            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, path), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("[Hepsilojistik] GetInboundStatus failed: {Status} — {Error}",
                    response.StatusCode, error);
                return new InboundStatus(shipmentId, "UNKNOWN", 0, 0);
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var status = doc.RootElement.TryGetProperty("status", out var st)
                ? st.GetString() ?? "UNKNOWN"
                : "UNKNOWN";

            var totalExpected = doc.RootElement.TryGetProperty("totalExpectedQuantity", out var teq)
                ? teq.GetInt32() : 0;
            var totalReceived = doc.RootElement.TryGetProperty("totalReceivedQuantity", out var trq)
                ? trq.GetInt32() : 0;

            DateTime? receivedAt = null;
            if (doc.RootElement.TryGetProperty("receivedAt", out var recAt) &&
                DateTime.TryParse(recAt.GetString(), out var parsedDate))
                receivedAt = parsedDate;

            _logger.LogInformation("[Hepsilojistik] Inbound status {ShipmentId}: {Status}", shipmentId, status);
            return new InboundStatus(shipmentId, status, totalExpected, totalReceived, receivedAt);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "[Hepsilojistik] Circuit breaker open — GetInboundStatus unavailable");
            return new InboundStatus(shipmentId, "UNAVAILABLE", 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Hepsilojistik] GetInboundStatus exception: {ShipmentId}", shipmentId);
            return new InboundStatus(shipmentId, "ERROR", 0, 0);
        }
    }

    /// <summary>
    /// Gets fulfillment orders shipped from Hepsilojistik warehouse since the specified date.
    /// GET /orders?since={since}&status=SHIPPED
    /// </summary>
    public async Task<IReadOnlyList<FulfillmentOrderResult>> GetFulfillmentOrdersAsync(
        DateTime since, CancellationToken ct = default)
    {
        _logger.LogInformation("[Hepsilojistik] GetFulfillmentOrders since {Since}", since);

        var orders = new List<FulfillmentOrderResult>();

        try
        {
            var page = 0;
            var hasMore = true;
            const int pageSize = 50;

            while (hasMore)
            {
                ct.ThrowIfCancellationRequested();

                var sinceStr = since.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var path = $"/orders?since={Uri.EscapeDataString(sinceStr)}" +
                           $"&page={page}&size={pageSize}";

                var response = await ExecuteWithRetryAsync(
                    () => new HttpRequestMessage(HttpMethod.Get, path), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("[Hepsilojistik] GetFulfillmentOrders failed: {Status} — {Error}",
                        response.StatusCode, error);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                var root = doc.RootElement;
                var ordersEl = root.ValueKind == JsonValueKind.Array
                    ? root
                    : root.TryGetProperty("orders", out var ords) ? ords
                    : root.TryGetProperty("data", out var data) ? data
                    : default;

                if (ordersEl.ValueKind != JsonValueKind.Array || ordersEl.GetArrayLength() == 0)
                {
                    hasMore = false;
                    break;
                }

                foreach (var order in ordersEl.EnumerateArray())
                {
                    var orderId = order.TryGetProperty("orderId", out var oid)
                        ? oid.GetString() ?? ""
                        : order.TryGetProperty("id", out var id2) ? id2.GetString() ?? "" : "";

                    var status = order.TryGetProperty("status", out var st)
                        ? st.GetString() ?? "UNKNOWN" : "UNKNOWN";

                    DateTime? shippedDate = null;
                    if (order.TryGetProperty("shippedDate", out var sd) &&
                        DateTime.TryParse(sd.GetString(), out var parsedDate))
                        shippedDate = parsedDate;

                    var trackingNumber = order.TryGetProperty("trackingNumber", out var tn)
                        ? tn.GetString() : null;
                    var carrierName = order.TryGetProperty("carrierName", out var cn)
                        ? cn.GetString() : null;

                    var items = new List<FulfillmentOrderItem>();
                    var itemsEl = order.TryGetProperty("items", out var orderItems) ? orderItems
                        : order.TryGetProperty("orderItems", out var oi2) ? oi2
                        : default;

                    if (itemsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in itemsEl.EnumerateArray())
                        {
                            var sku = item.TryGetProperty("merchantSku", out var skuEl)
                                ? skuEl.GetString() ?? ""
                                : item.TryGetProperty("sku", out var s2) ? s2.GetString() ?? "" : "";

                            var qtyOrdered = item.TryGetProperty("quantity", out var qo) ? qo.GetInt32()
                                : item.TryGetProperty("quantityOrdered", out var qo2) ? qo2.GetInt32() : 0;
                            var qtyShipped = item.TryGetProperty("quantityShipped", out var qs) ? qs.GetInt32()
                                : qtyOrdered;

                            if (!string.IsNullOrEmpty(sku))
                                items.Add(new FulfillmentOrderItem(sku, qtyOrdered, qtyShipped));
                        }
                    }

                    if (!string.IsNullOrEmpty(orderId))
                        orders.Add(new FulfillmentOrderResult(
                            orderId, status, items.AsReadOnly(), shippedDate, trackingNumber, carrierName));
                }

                // Check if there are more pages
                hasMore = ordersEl.GetArrayLength() >= pageSize;
                page++;
            }

            _logger.LogInformation("[Hepsilojistik] GetFulfillmentOrders complete: {Count} orders", orders.Count);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "[Hepsilojistik] Circuit breaker open — GetFulfillmentOrders returning empty");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Hepsilojistik] GetFulfillmentOrders exception");
        }

        return orders.AsReadOnly();
    }

    /// <summary>
    /// Health check: lightweight GET to Hepsilojistik API (uses /inventory?limit=1 as proxy).
    /// </summary>
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, "/inventory?limit=1"), ct).ConfigureAwait(false);

            var available = response.IsSuccessStatusCode || (int)response.StatusCode < 500;
            _logger.LogInformation("[Hepsilojistik] IsAvailable: {Available} ({Status})",
                available, response.StatusCode);
            return available;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Hepsilojistik] IsAvailable check failed");
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // HTTP helper — with rate limiting + circuit breaker recovery
    // ═══════════════════════════════════════════

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
            _logger.LogWarning(ex, "[Hepsilojistik] Circuit breaker is open — returning 503");
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
}
