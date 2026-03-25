using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using System.Net;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Ciceksepeti platform adaptoru — Dalga 3 tam entegrasyon.
/// IIntegratorAdapter + IWebhookCapableAdapter + IOrderCapableAdapter + IShipmentCapableAdapter
/// x-api-key auth, Polly retry, SemaphoreSlim rate limiting.
/// </summary>
public class CiceksepetiAdapter : IIntegratorAdapter, IWebhookCapableAdapter,
    IOrderCapableAdapter, IShipmentCapableAdapter, ISettlementCapableAdapter, IClaimCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CiceksepetiAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);

    private bool _isConfigured;

    public CiceksepetiAdapter(HttpClient httpClient, ILogger<CiceksepetiAdapter> logger)
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
                        "Ciceksepeti API retry {Attempt} after {Delay}ms",
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

    public string PlatformCode => nameof(PlatformType.Ciceksepeti);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ── Auth ────────────────────────────────────────────
    private void ConfigureAuth(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var apiKey = credentials.GetValueOrDefault("ApiKey", "");

        _httpClient.DefaultRequestHeaders.Remove("x-api-key");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MesTech-Ciceksepeti-Client/3.0");

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _httpClient.BaseAddress = new Uri(credentials["BaseUrl"], UriKind.Absolute);

        _isConfigured = true;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "CiceksepetiAdapter henuz konfigure edilmedi. Once TestConnectionAsync cagirin.");
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
            if (!credentials.ContainsKey("ApiKey") || string.IsNullOrWhiteSpace(credentials["ApiKey"]))
            {
                result.ErrorMessage = "ApiKey alani zorunludur.";
                result.ResponseTime = sw.Elapsed;
                return result;
            }

            ConfigureAuth(credentials);

            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, "/api/v1/Products?PageSize=1&Page=1"), ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;
            sw.Stop();
            result.ResponseTime = sw.Elapsed;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);
                var totalCount = doc.RootElement.TryGetProperty("totalCount", out var tc) ? tc.GetInt32() : 0;

                result.IsSuccess = true;
                result.ProductCount = totalCount;
                result.StoreName = "Ciceksepeti Magaza";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Yetkisiz erisim — API Key hatali.",
                    System.Net.HttpStatusCode.Forbidden => "Erisim engellendi — API Key yetkisiz.",
                    _ => $"Ciceksepeti API hatasi: {response.StatusCode} - {errorContent}"
                };
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            result.ErrorMessage = $"Baglanti hatasi: {ex.Message}";
            result.ResponseTime = sw.Elapsed;
            _logger.LogError(ex, "Ciceksepeti TestConnection failed");
        }

        return result;
    }

    // ── Product methods ─────────────────────────────────
    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(product);

        var payload = new
        {
            stockCode = product.SKU,
            productName = product.Name,
            salesPrice = product.SalePrice,
            stockQuantity = product.Stock,
            description = product.Description ?? "",
            barcode = product.Barcode
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Products");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ciceksepeti PushProduct OK: {Sku}", product.SKU);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Ciceksepeti PushProduct failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        var products = new List<Product>();
        var page = 1;
        const int pageSize = 50;

        while (!ct.IsCancellationRequested)
        {
            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, $"/api/v1/Products?PageSize={pageSize}&Page={page}"), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Ciceksepeti PullProducts page {Page} failed: {Status} {Error}", page, response.StatusCode, errorBody);
                break;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var csResponse = JsonSerializer.Deserialize<CsProductListResponse>(content, _jsonOptions);

            if (csResponse?.Products is null || csResponse.Products.Count == 0)
                break;

            foreach (var cp in csResponse.Products)
            {
                products.Add(new Product
                {
                    Name = cp.ProductName,
                    SKU = cp.StockCode,
                    Barcode = cp.Barcode,
                    Description = cp.Description,
                    SalePrice = cp.SalesPrice,
                    Stock = cp.StockQuantity,
                    IsActive = true
                });
            }

            if (products.Count >= csResponse.TotalCount)
                break;

            page++;
        }

        _logger.LogInformation("Ciceksepeti PullProducts: {Count} products fetched", products.Count);
        return products.AsReadOnly();
    }

    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();

        var payload = new { items = new[] { new { stockCode = productId.ToString(), quantity = newStock } } };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "/api/v1/Products/stock");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ciceksepeti stock update OK: {ProductId} → {Stock}", productId, newStock);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Ciceksepeti stock update failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();

        var payload = new { items = new[] { new { stockCode = productId.ToString(), salesPrice = newPrice } } };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "/api/v1/Products/price");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ciceksepeti price update OK: {ProductId} → {Price}", productId, newPrice);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Ciceksepeti price update failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Order methods ───────────────────────────────────
    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        var orders = new List<ExternalOrderDto>();
        var page = 1;
        const int pageSize = 50;

        var url = "/api/v1/Order?PageSize=" + pageSize;
        if (since.HasValue)
            url += "&StartDate=" + since.Value.ToString("yyyy-MM-dd");

        while (!ct.IsCancellationRequested)
        {
            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, $"{url}&Page={page}"), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Ciceksepeti PullOrders page {Page} failed: {Status} {Error}", page, response.StatusCode, errorBody);
                break;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var csResponse = JsonSerializer.Deserialize<CsOrderListResponse>(content, _jsonOptions);

            if (csResponse?.Orders is null || csResponse.Orders.Count == 0)
                break;

            foreach (var csOrder in csResponse.Orders)
            {
                // Sub-order flattening: her sub-order ayri ExternalOrderDto olur
                foreach (var sub in csOrder.SubOrders)
                {
                    orders.Add(new ExternalOrderDto
                    {
                        PlatformOrderId = sub.SubOrderId.ToString(),
                        PlatformCode = PlatformCode,
                        OrderNumber = csOrder.OrderNumber,
                        Status = sub.Status,
                        CustomerName = csOrder.CustomerName,
                        CustomerEmail = csOrder.CustomerEmail,
                        CustomerAddress = csOrder.DeliveryAddress,
                        CustomerCity = csOrder.DeliveryCity,
                        TotalAmount = sub.TotalPrice,
                        CargoProviderName = sub.CargoCompany,
                        CargoTrackingNumber = sub.TrackingNumber,
                        ShipmentPackageId = sub.SubOrderId.ToString(),
                        OrderDate = csOrder.OrderDate,
                        Lines = sub.Items.Select(item => new ExternalOrderLineDto
                        {
                            PlatformLineId = item.ItemId.ToString(),
                            SKU = item.StockCode,
                            Barcode = item.Barcode,
                            ProductName = item.ProductName,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            LineTotal = item.TotalPrice
                        }).ToList()
                    });
                }
            }

            if (orders.Count >= csResponse.TotalCount)
                break;

            page++;
        }

        _logger.LogInformation("Ciceksepeti PullOrders: {Count} sub-orders fetched", orders.Count);
        return orders.AsReadOnly();
    }

    public async Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default)
    {
        EnsureConfigured();

        var payload = new { subOrderId = long.Parse(packageId), status };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "/api/v1/Order/Status");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ciceksepeti order status updated: {PackageId} → {Status}", packageId, status);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Ciceksepeti order status update failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Webhook methods ─────────────────────────────────
    public Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        _logger.LogInformation("Ciceksepeti webhook registration not supported via API — manual panel config");
        return Task.FromResult(false);
    }

    public Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
        => Task.FromResult(false);

    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        var webhook = JsonSerializer.Deserialize<CsWebhookPayload>(payload, _jsonOptions);
        _logger.LogInformation("Ciceksepeti webhook received: {EventType} Order={OrderId} Sub={SubOrderId}",
            webhook?.EventType, webhook?.OrderId, webhook?.SubOrderId);
        return Task.CompletedTask;
    }

    // ── Shipment notification ───────────────────────────
    public async Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber,
        CargoProvider provider, CancellationToken ct = default)
    {
        EnsureConfigured();

        var cargoCompany = provider switch
        {
            CargoProvider.YurticiKargo => "Yurtiçi Kargo",
            CargoProvider.ArasKargo => "Aras Kargo",
            CargoProvider.SuratKargo => "Sürat Kargo",
            _ => provider.ToString()
        };

        var payload = new
        {
            subOrderId = long.Parse(platformOrderId),
            cargoCompany,
            trackingNumber
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Order/Shipping");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ciceksepeti shipment sent: SubOrder={SubOrderId} Tracking={Tracking}",
                platformOrderId, trackingNumber);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Ciceksepeti shipment failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Return / Cancel methods (K1c-06/07/08) ────────
    /// <summary>
    /// K1c-06: Fetch returns from Ciceksepeti API.
    /// </summary>
    public async Task<IReadOnlyList<CsReturnDto>> GetReturnsAsync(DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        var returns = new List<CsReturnDto>();
        var page = 1;
        const int pageSize = 50;

        var url = $"/api/v1/Order/Returns?PageSize={pageSize}";
        if (since.HasValue)
            url += "&StartDate=" + since.Value.ToString("yyyy-MM-dd");

        while (!ct.IsCancellationRequested)
        {
            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, $"{url}&Page={page}"), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Ciceksepeti GetReturns page {Page} failed: {Status} {Error}", page, response.StatusCode, errorBody);
                break;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var csResponse = JsonSerializer.Deserialize<CsReturnListResponse>(content, _jsonOptions);

            if (csResponse?.Returns is null || csResponse.Returns.Count == 0)
                break;

            returns.AddRange(csResponse.Returns);

            if (returns.Count >= csResponse.TotalCount)
                break;

            page++;
        }

        _logger.LogInformation("Ciceksepeti GetReturns: {Count} returns fetched", returns.Count);
        return returns.AsReadOnly();
    }

    /// <summary>
    /// K1c-07: Approve a return on Ciceksepeti.
    /// </summary>
    public async Task<bool> ApproveReturnAsync(long returnId, CancellationToken ct = default)
    {
        EnsureConfigured();

        var payload = new { returnId, status = "Approved" };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "/api/v1/Order/Returns/Approve");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ciceksepeti return approved: {ReturnId}", returnId);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Ciceksepeti return approval failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    /// <summary>
    /// K1c-08: Cancel an order on Ciceksepeti.
    /// </summary>
    public async Task<bool> CancelOrderAsync(long subOrderId, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();

        var payload = new { subOrderId, cancelReason = reason };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "/api/v1/Order/Cancel");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ciceksepeti order cancelled: SubOrder={SubOrderId} Reason={Reason}",
                subOrderId, reason);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Ciceksepeti cancel failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Product Update / Delete ────────────────────────

    /// <summary>
    /// PUT /api/v1/Products — Urun bilgilerini gunceller.
    /// </summary>
    public async Task<bool> UpdateProductAsync(CsProductUpdateDto product, CancellationToken ct = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(product);

        var json = JsonSerializer.Serialize(product, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "/api/v1/Products");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ciceksepeti product updated: {ProductId}", product.ProductId);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Ciceksepeti product update failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    /// <summary>
    /// DELETE /api/v1/Products/{productId} — Urunu siler.
    /// </summary>
    public async Task<bool> DeleteProductAsync(string productId, CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/Products/{productId}"), ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ciceksepeti product deleted: {ProductId}", productId);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Ciceksepeti product delete failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Categories (Platform-Specific) ──────────────────

    /// <summary>
    /// GET /api/v1/Categories — Ciceksepeti kategori listesini ceker.
    /// </summary>
    public async Task<List<CsCategoryDto>> GetCsCategoriesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/v1/Categories"), ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Ciceksepeti GetCategories failed {Status}: {Error}", response.StatusCode, error);
            return new List<CsCategoryDto>();
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var csResponse = JsonSerializer.Deserialize<CsCategoryListResponse>(content, _jsonOptions);

        _logger.LogInformation("Ciceksepeti GetCategories: {Count} categories fetched",
            csResponse?.Categories.Count ?? 0);
        return csResponse?.Categories ?? new List<CsCategoryDto>();
    }

    /// <summary>
    /// GET /api/v1/Categories/{categoryId}/attributes — Kategori attribute'larini ceker.
    /// </summary>
    public async Task<List<CsAttributeDto>> GetCategoryAttributesAsync(string categoryId, CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/v1/Categories/{categoryId}/attributes"), ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Ciceksepeti GetCategoryAttributes failed {Status}: {Error}",
                response.StatusCode, error);
            return new List<CsAttributeDto>();
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var csResponse = JsonSerializer.Deserialize<CsAttributeListResponse>(content, _jsonOptions);

        _logger.LogInformation("Ciceksepeti GetCategoryAttributes: {CategoryId} → {Count} attributes",
            categoryId, csResponse?.Attributes.Count ?? 0);
        return csResponse?.Attributes ?? new List<CsAttributeDto>();
    }

    // ── Batch Operations ────────────────────────────────

    /// <summary>
    /// PUT /api/v1/Products/stock/batch — Toplu stok guncelleme.
    /// </summary>
    public async Task<bool> BatchUpdateStockAsync(List<CsStockUpdate> items, CancellationToken ct = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(items);

        var payload = new { items };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "/api/v1/Products/stock/batch");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ciceksepeti batch stock update OK: {Count} items", items.Count);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Ciceksepeti batch stock update failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    /// <summary>
    /// PUT /api/v1/Products/price/batch — Toplu fiyat guncelleme.
    /// </summary>
    public async Task<bool> BatchUpdatePriceAsync(List<CsPriceUpdate> items, CancellationToken ct = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(items);

        var payload = new { items };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, "/api/v1/Products/price/batch");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ciceksepeti batch price update OK: {Count} items", items.Count);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Ciceksepeti batch price update failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Cargo Tracking ──────────────────────────────────

    /// <summary>
    /// GET /api/v1/Order/Tracking — Kargo takip bilgisi sorgular.
    /// </summary>
    public async Task<CsTrackingDto?> GetCargoTrackingAsync(string orderId, CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/v1/Order/Tracking?orderId={orderId}"), ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Ciceksepeti GetCargoTracking failed {Status}: {Error}", response.StatusCode, error);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var tracking = JsonSerializer.Deserialize<CsTrackingDto>(content, _jsonOptions);

        _logger.LogInformation("Ciceksepeti cargo tracking: Order={OrderId} → {Status}",
            orderId, tracking?.Status);
        return tracking;
    }

    // ── Categories (Interface) ──────────────────────────
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var csCategories = await GetCsCategoriesAsync(ct).ConfigureAwait(false);
        if (csCategories.Count == 0)
            return Array.Empty<CategoryDto>();

        var lookup = csCategories.ToLookup(c => c.ParentId);
        var roots = csCategories.Where(c => c.ParentId is null).ToList();

        CategoryDto Map(CsCategoryDto cs)
        {
            var dto = new CategoryDto
            {
                PlatformCategoryId = (int)cs.Id,
                Name = cs.Name,
                ParentId = cs.ParentId.HasValue ? (int)cs.ParentId.Value : null
            };
            foreach (var child in lookup[cs.Id])
                dto.SubCategories.Add(Map(child));
            return dto;
        }

        var result = roots.Select(Map).ToList();
        _logger.LogInformation("Ciceksepeti GetCategoriesAsync: {Count} top-level categories mapped", result.Count);
        return result.AsReadOnly();
    }

    // ── Settlement (ISettlementCapableAdapter) ────────
    /// <summary>
    /// GET /api/v1/settlement — Ciceksepeti cari hesap ekstresi.
    /// </summary>
    public async Task<SettlementDto?> GetSettlementAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            var url = $"/api/v1/settlement?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, url), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Ciceksepeti GetSettlement failed {Status}: {Error}", response.StatusCode, error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var settlement = new SettlementDto
            {
                PlatformCode = "Ciceksepeti",
                StartDate = startDate,
                EndDate = endDate,
                TotalSales = root.TryGetProperty("totalSales", out var ts) ? ts.GetDecimal() : 0m,
                TotalCommission = root.TryGetProperty("totalCommission", out var tc) ? tc.GetDecimal() : 0m,
                TotalShippingCost = root.TryGetProperty("totalShippingCost", out var tsc) ? tsc.GetDecimal() : 0m,
                TotalReturnDeduction = root.TryGetProperty("totalReturnDeduction", out var trd) ? trd.GetDecimal() : 0m,
                NetAmount = root.TryGetProperty("netAmount", out var na) ? na.GetDecimal() : 0m,
                Currency = root.TryGetProperty("currency", out var cur) ? cur.GetString() ?? "TRY" : "TRY"
            };

            if (root.TryGetProperty("lines", out var linesEl) && linesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var line in linesEl.EnumerateArray())
                {
                    settlement.Lines.Add(new SettlementLineDto
                    {
                        OrderNumber = line.TryGetProperty("orderNumber", out var on) ? on.GetString() : null,
                        TransactionType = line.TryGetProperty("transactionType", out var tt) ? tt.GetString() : null,
                        Amount = line.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0m,
                        CommissionAmount = line.TryGetProperty("commissionAmount", out var ca) ? ca.GetDecimal() : null,
                        TransactionDate = line.TryGetProperty("transactionDate", out var td) && td.TryGetDateTime(out var dt)
                            ? dt : startDate
                    });
                }
            }

            _logger.LogInformation("Ciceksepeti GetSettlement: {LineCount} lines, net={NetAmount}",
                settlement.Lines.Count, settlement.NetAmount);
            return settlement;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ciceksepeti GetSettlement exception for {StartDate}–{EndDate}", startDate, endDate);
            return null;
        }
    }

    /// <summary>
    /// Ciceksepeti cargo invoices — separate API not publicly available; returns empty list.
    /// </summary>
    public Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(DateTime startDate, CancellationToken ct = default)
    {
        _logger.LogInformation("Ciceksepeti GetCargoInvoices: cargo invoice API not available — returning empty list");
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
            var url = "/api/v1/returns";
            if (since.HasValue)
                url += "?startDate=" + since.Value.ToString("yyyy-MM-dd'T'HH:mm:ss");

            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, url), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Ciceksepeti PullClaims failed: {Status} {Error}",
                    response.StatusCode, errorBody);
                return claims;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var items = doc.RootElement.TryGetProperty("data", out var dataEl)
                ? dataEl.EnumerateArray()
                : doc.RootElement.EnumerateArray();

            foreach (var item in items)
            {
                claims.Add(new ExternalClaimDto
                {
                    PlatformClaimId = item.TryGetProperty("id", out var idEl) ? idEl.ToString() : string.Empty,
                    PlatformCode = PlatformCode,
                    OrderNumber = item.TryGetProperty("orderNo", out var onEl) ? onEl.GetString() ?? string.Empty : string.Empty,
                    Status = item.TryGetProperty("status", out var stEl) ? stEl.GetString() ?? string.Empty : string.Empty,
                    Reason = item.TryGetProperty("reason", out var rsEl) ? rsEl.GetString() ?? string.Empty : string.Empty,
                    ReasonDetail = item.TryGetProperty("reasonDetail", out var rdEl) ? rdEl.GetString() : null,
                    CustomerName = item.TryGetProperty("customerName", out var cnEl) ? cnEl.GetString() ?? string.Empty : string.Empty,
                    CustomerEmail = item.TryGetProperty("customerEmail", out var ceEl) ? ceEl.GetString() : null,
                    Amount = item.TryGetProperty("totalAmount", out var amEl) && amEl.TryGetDecimal(out var amt) ? amt : 0m,
                    Currency = "TRY",
                    ClaimDate = item.TryGetProperty("createdDate", out var cdEl) && cdEl.TryGetDateTime(out var cd) ? cd : DateTime.UtcNow,
                    ResolvedDate = item.TryGetProperty("resolvedDate", out var rvEl) && rvEl.TryGetDateTime(out var rv) ? rv : null,
                    Lines = ParseCsClaimLines(item)
                });
            }

            _logger.LogInformation("Ciceksepeti PullClaims: {Count} claims fetched", claims.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ciceksepeti PullClaims exception");
        }

        return claims;
    }

    /// <inheritdoc />
    public async Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Put, $"/api/v1/returns/{claimId}/approve"), ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ciceksepeti claim {ClaimId} approved", claimId);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Ciceksepeti claim {ClaimId} approve failed: {Status} {Error}",
                claimId, response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ciceksepeti ApproveClaimAsync exception for {ClaimId}", claimId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            var body = JsonSerializer.Serialize(new { reason }, _jsonOptions);
            var response = await ExecuteWithRetryAsync(
                () =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/returns/{claimId}/reject")
                    {
                        Content = new StringContent(body, Encoding.UTF8, "application/json")
                    };
                    return req;
                }, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ciceksepeti claim {ClaimId} rejected with reason: {Reason}", claimId, reason);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Ciceksepeti claim {ClaimId} reject failed: {Status} {Error}",
                claimId, response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ciceksepeti RejectClaimAsync exception for {ClaimId}", claimId);
            return false;
        }
    }

    private static List<ExternalClaimLineDto> ParseCsClaimLines(JsonElement item)
    {
        var lines = new List<ExternalClaimLineDto>();
        if (!item.TryGetProperty("lines", out var linesEl) && !item.TryGetProperty("items", out linesEl))
            return lines;

        foreach (var line in linesEl.EnumerateArray())
        {
            lines.Add(new ExternalClaimLineDto
            {
                SKU = line.TryGetProperty("sku", out var skuEl) ? skuEl.GetString() : null,
                Barcode = line.TryGetProperty("barcode", out var bcEl) ? bcEl.GetString() : null,
                ProductName = line.TryGetProperty("productName", out var pnEl) ? pnEl.GetString() ?? string.Empty : string.Empty,
                Quantity = line.TryGetProperty("quantity", out var qEl) && qEl.TryGetInt32(out var q) ? q : 1,
                UnitPrice = line.TryGetProperty("unitPrice", out var upEl) && upEl.TryGetDecimal(out var up) ? up : 0m
            });
        }

        return lines;
    }

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
}
