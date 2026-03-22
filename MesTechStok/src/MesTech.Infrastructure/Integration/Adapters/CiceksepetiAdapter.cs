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
    IOrderCapableAdapter, IShipmentCapableAdapter
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
                _logger.LogWarning("Ciceksepeti PullProducts page {Page} failed: {Status}", page, response.StatusCode);
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
                _logger.LogWarning("Ciceksepeti PullOrders page {Page} failed: {Status}", page, response.StatusCode);
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
                _logger.LogWarning("Ciceksepeti GetReturns page {Page} failed: {Status}", page, response.StatusCode);
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
    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());

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
