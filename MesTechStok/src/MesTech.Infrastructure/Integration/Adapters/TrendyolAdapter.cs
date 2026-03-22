using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Trendyol platform adaptoru — TAM entegrasyon.
/// IIntegratorAdapter + IWebhookCapableAdapter + IOrderCapableAdapter
/// + IInvoiceCapableAdapter + IClaimCapableAdapter + ISettlementCapableAdapter
/// Rate limiting, Polly retry, Basic Auth mevcut koddan alinmistir.
/// </summary>
public class TrendyolAdapter : IIntegratorAdapter, IWebhookCapableAdapter,
    IOrderCapableAdapter, IInvoiceCapableAdapter, IClaimCapableAdapter, ISettlementCapableAdapter,
    IPingableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TrendyolAdapter> _logger;
    private readonly TrendyolOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(100, 100);

    // Credential key'leri — StoreCredential tablosundaki Key alanlari
    private string? _supplierId;
    private bool _isConfigured;

    public TrendyolAdapter(HttpClient httpClient, ILogger<TrendyolAdapter> logger,
        IOptions<TrendyolOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new TrendyolOptions();

        // Initialise BaseAddress from options so sandbox toggle works without credential override
        _httpClient.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            // HTTP 429 rate-limit retry — uses Retry-After header or defaults to 11s (Trendyol: 50 req/10s)
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 5,
                DelayGenerator = args =>
                {
                    if (args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests } retryResponse
                        && retryResponse.Headers.RetryAfter is { } retryAfter)
                    {
                        var delay = retryAfter.Delta ?? TimeSpan.FromSeconds(11);
                        return new ValueTask<TimeSpan?>(delay);
                    }
                    // Not a 429 — fall through to next retry strategy
                    return new ValueTask<TimeSpan?>(TimeSpan.Zero);
                },
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => r.StatusCode == HttpStatusCode.TooManyRequests),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Trendyol API rate limited (429). Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning(
                        "Trendyol API retry {Attempt} after {Delay}ms",
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

    public string PlatformCode => nameof(PlatformType.Trendyol);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    private void ConfigureAuth(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var apiKey = credentials.GetValueOrDefault("ApiKey", "");
        var apiSecret = credentials.GetValueOrDefault("ApiSecret", "");
        _supplierId = credentials.GetValueOrDefault("SupplierId", "");

        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);

        // Support BaseUrl override for sandbox testing via credentials
        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _httpClient.BaseAddress = new Uri(credentials["BaseUrl"], UriKind.Absolute);

        // UseSandbox=true shortcut sets sandbox URL automatically
        if (credentials.GetValueOrDefault("UseSandbox", "false").Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            _httpClient.BaseAddress = new Uri(_options.SandboxBaseUrl, UriKind.Absolute);
        }

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MesTech-Trendyol-Client/3.0");
        _isConfigured = true;
    }

    public async Task<ConnectionTestResultDto> TestConnectionAsync(
        Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var sw = Stopwatch.StartNew();
        var result = new ConnectionTestResultDto { PlatformCode = PlatformCode };

        try
        {
            // Credential dogrulama
            if (!credentials.ContainsKey("ApiKey") || string.IsNullOrWhiteSpace(credentials["ApiKey"]) ||
                !credentials.ContainsKey("ApiSecret") || string.IsNullOrWhiteSpace(credentials["ApiSecret"]) ||
                !credentials.ContainsKey("SupplierId") || string.IsNullOrWhiteSpace(credentials["SupplierId"]))
            {
                result.ErrorMessage = "ApiKey, ApiSecret ve SupplierId alanlari zorunludur.";
                result.ResponseTime = sw.Elapsed;
                return result;
            }

            ConfigureAuth(credentials);

            // Trendyol API'ye test istegi — urun sayisini cek
            var response = await _httpClient.GetAsync(
                new Uri($"/integration/product/sellers/{_supplierId}/products?page=0&size=1", UriKind.Relative), ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;
            sw.Stop();
            result.ResponseTime = sw.Elapsed;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);
                var totalElements = doc.RootElement.TryGetProperty("totalElements", out var te) ? te.GetInt32() : 0;

                result.IsSuccess = true;
                result.ProductCount = totalElements;
                result.StoreName = $"Trendyol - Supplier {_supplierId}";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Yetkisiz erisim — API Key/Secret hatali.",
                    System.Net.HttpStatusCode.Forbidden => "Erisim engellendi — Supplier ID hatali olabilir.",
                    _ => $"Trendyol API hatasi: {response.StatusCode} - {errorContent}"
                };
            }
        }
        catch (TaskCanceledException)
        {
            result.ErrorMessage = "Baglanti zaman asimina ugradi.";
            result.ResponseTime = sw.Elapsed;
        }
        catch (HttpRequestException ex)
        {
            result.ErrorMessage = $"Baglanti hatasi: {ex.Message}";
            result.ResponseTime = sw.Elapsed;
        }

        _logger.LogInformation("Trendyol connection test: Success={Success}, Time={Time}ms",
            result.IsSuccess, result.ResponseTime.TotalMilliseconds);
        return result;
    }

    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.PushProductAsync SKU: {SKU}", product.SKU);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            // brandId must be integer for Trendyol API — resolve from BrandPlatformMapping
            var platformBrandId = 0; // Default; caller should set via BrandPlatformMapping
            if (product.BrandEntity != null)
            {
                // BrandDto.PlatformBrandId is already int
            }

            var payload = new
            {
                items = new[]
                {
                    new
                    {
                        barcode = product.Barcode ?? product.SKU,
                        title = product.Name,
                        productMainId = product.SKU,
                        brandId = platformBrandId,
                        categoryId = product.CategoryId,
                        quantity = product.Stock,
                        stockCode = product.SKU,
                        description = product.Description ?? "",
                        currencyType = product.CurrencyCode,
                        listPrice = product.ListPrice ?? product.SalePrice,
                        salePrice = product.SalePrice,
                        vatRate = (int)(product.TaxRate * 100)
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/product/sellers/{_supplierId}/v2/products", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol PushProduct failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol PushProduct success: {SKU}", product.SKU);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol PushProduct exception: {SKU}", product.SKU);
            return false;
        }
    }

    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.PullProductsAsync called");

        var products = new List<Product>();
        var page = 0;
        const int pageSize = 50;

        try
        {
            bool hasMore = true;
            while (hasMore)
            {
                await ApplyRateLimitAsync(ct).ConfigureAwait(false);

                var response = await _retryPipeline.ExecuteAsync(
                    async token => await _httpClient.GetAsync(
                        new Uri($"/integration/product/sellers/{_supplierId}/products?page={page}&size={pageSize}", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                var totalPages = doc.RootElement.TryGetProperty("totalPages", out var tp) ? tp.GetInt32() : 0;

                if (doc.RootElement.TryGetProperty("content", out var contentArr))
                {
                    foreach (var item in contentArr.EnumerateArray())
                    {
                        products.Add(new Product
                        {
                            Name = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                            SKU = item.TryGetProperty("stockCode", out var sc) ? sc.GetString() ?? "" : "",
                            Barcode = item.TryGetProperty("barcode", out var b) ? b.GetString() : null,
                            SalePrice = item.TryGetProperty("salePrice", out var sp) ? sp.GetDecimal() : 0,
                            ListPrice = item.TryGetProperty("listPrice", out var lp) ? lp.GetDecimal() : null,
                            Stock = item.TryGetProperty("quantity", out var q) ? q.GetInt32() : 0,
                            Description = item.TryGetProperty("description", out var d) ? d.GetString() : null
                        });
                    }
                }

                page++;
                hasMore = page < totalPages;
            }

            _logger.LogInformation("Trendyol PullProducts: {Count} products retrieved", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol PullProducts failed at page {Page}", page);
        }

        return products.AsReadOnly();
    }

    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.PushStockUpdateAsync: ProductId={ProductId} qty={Qty}", productId, newStock);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                items = new[]
                {
                    new { barcode = productId.ToString(), quantity = newStock }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/inventory/sellers/{_supplierId}/products/price-and-inventory", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol StockUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol StockUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.PushPriceUpdateAsync: ProductId={ProductId} price={Price}", productId, newPrice);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                items = new[]
                {
                    new { barcode = productId.ToString(), listPrice = newPrice, salePrice = newPrice }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/inventory/sellers/{_supplierId}/products/price-and-inventory", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol PriceUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol PriceUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // IOrderCapableAdapter — Siparis Entegrasyonu
    // ═══════════════════════════════════════════

    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.PullOrdersAsync since={Since}", since);

        var orders = new List<ExternalOrderDto>();
        var page = 0;
        const int pageSize = 50;

        try
        {
            bool hasMore = true;
            while (hasMore)
            {
                await ApplyRateLimitAsync(ct).ConfigureAwait(false);

                var url = $"/integration/order/sellers/{_supplierId}/orders?page={page}&size={pageSize}&orderByField=CreatedDate&orderByDirection=DESC";
                if (since.HasValue)
                {
                    var epoch = new DateTimeOffset(since.Value).ToUnixTimeMilliseconds();
                    url += $"&startDate={epoch}";
                }

                var response = await _retryPipeline.ExecuteAsync(
                    async token => await _httpClient.GetAsync(new Uri(url, UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                var totalPages = doc.RootElement.TryGetProperty("totalPages", out var tp) ? tp.GetInt32() : 0;

                if (doc.RootElement.TryGetProperty("content", out var contentArr))
                {
                    foreach (var item in contentArr.EnumerateArray())
                    {
                        var orderNumber = item.TryGetProperty("orderNumber", out var onProp) ? onProp.GetString() ?? "" : "";

                        var order = new ExternalOrderDto
                        {
                            PlatformCode = PlatformCode,
                            PlatformOrderId = orderNumber,
                            OrderNumber = orderNumber,
                            Status = item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
                            CustomerName = BuildTrendyolCustomerName(item),
                            TotalAmount = item.TryGetProperty("totalPrice", out var tp2) ? tp2.GetDecimal() : 0,
                            OrderDate = item.TryGetProperty("orderDate", out var od) ? DateTimeOffset.FromUnixTimeMilliseconds(od.GetInt64()).UtcDateTime : DateTime.UtcNow
                        };

                        // Siparis satir detaylari
                        if (item.TryGetProperty("lines", out var lines))
                        {
                            foreach (var line in lines.EnumerateArray())
                            {
                                order.Lines.Add(new ExternalOrderLineDto
                                {
                                    PlatformLineId = line.TryGetProperty("id", out var lid) ? lid.GetInt64().ToString() : null,
                                    SKU = line.TryGetProperty("merchantSku", out var sku) ? sku.GetString() : null,
                                    Barcode = line.TryGetProperty("barcode", out var bc) ? bc.GetString() : null,
                                    ProductName = line.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "",
                                    Quantity = line.TryGetProperty("quantity", out var qty) ? qty.GetInt32() : 1,
                                    UnitPrice = line.TryGetProperty("price", out var up) ? up.GetDecimal() : 0,
                                    DiscountAmount = line.TryGetProperty("discount", out var disc) ? disc.GetDecimal() : null,
                                    LineTotal = line.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0
                                });
                            }
                        }

                        // Kargo bilgisi
                        if (item.TryGetProperty("shipmentPackageId", out var spId))
                            order.ShipmentPackageId = spId.GetInt64().ToString();
                        if (item.TryGetProperty("cargoProviderName", out var cpn))
                            order.CargoProviderName = cpn.GetString();
                        if (item.TryGetProperty("cargoTrackingNumber", out var ctn))
                            order.CargoTrackingNumber = ctn.GetString();

                        orders.Add(order);
                    }
                }

                page++;
                hasMore = page < totalPages;
            }

            _logger.LogInformation("Trendyol PullOrders: {Count} orders retrieved", orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol PullOrders failed at page {Page}", page);
        }

        return orders.AsReadOnly();
    }

    public async Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.UpdateOrderStatusAsync: Package={PackageId} Status={Status}", packageId, status);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new { status, lines = Array.Empty<object>() };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PutAsync(
                        new Uri($"/integration/order/sellers/{_supplierId}/orders/shipment-packages/{packageId}", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol UpdateOrderStatus failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol UpdateOrderStatus exception: {PackageId}", packageId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // IInvoiceCapableAdapter — Fatura Gonderme
    // ═══════════════════════════════════════════

    public async Task<bool> SendInvoiceLinkAsync(string shipmentPackageId, string invoiceUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.SendInvoiceLinkAsync: Package={PackageId}", shipmentPackageId);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            if (!long.TryParse(shipmentPackageId, out var packageIdLong))
            {
                _logger.LogError("SendInvoiceLink: Invalid shipmentPackageId '{Id}'", shipmentPackageId);
                return false;
            }

            var payload = new { shipmentPackageId = packageIdLong, invoiceLink = invoiceUrl };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/order/sellers/{_supplierId}/orders/invoiceLinks", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol SendInvoiceLink failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol SendInvoiceLink exception: {PackageId}", shipmentPackageId);
            return false;
        }
    }

    public async Task<bool> SendInvoiceFileAsync(string shipmentPackageId, byte[] pdfBytes, string fileName, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.SendInvoiceFileAsync: Package={PackageId} File={FileName}", shipmentPackageId, fileName);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            using var formContent = new MultipartFormDataContent();
            formContent.Add(new StringContent(shipmentPackageId), "shipmentPackageId");
            formContent.Add(new ByteArrayContent(pdfBytes), "file", fileName);

            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.PostAsync(
                    new Uri($"/integration/order/sellers/{_supplierId}/orders/invoice-file", UriKind.Relative), formContent, token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol SendInvoiceFile failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol SendInvoiceFile exception: {PackageId}", shipmentPackageId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // IClaimCapableAdapter — Iade Entegrasyonu
    // ═══════════════════════════════════════════

    public async Task<IReadOnlyList<ExternalClaimDto>> PullClaimsAsync(DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.PullClaimsAsync since={Since}", since);

        var claims = new List<ExternalClaimDto>();
        var page = 0;
        const int pageSize = 50;

        try
        {
            bool hasMore = true;
            while (hasMore)
            {
                await ApplyRateLimitAsync(ct).ConfigureAwait(false);

                var url = $"/integration/order/sellers/{_supplierId}/claims?page={page}&size={pageSize}";
                if (since.HasValue)
                {
                    var epoch = new DateTimeOffset(since.Value).ToUnixTimeMilliseconds();
                    url += $"&claimDate={epoch}";
                }

                var response = await _retryPipeline.ExecuteAsync(
                    async token => await _httpClient.GetAsync(new Uri(url, UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                var totalPages = doc.RootElement.TryGetProperty("totalPages", out var tp) ? tp.GetInt32() : 0;

                if (doc.RootElement.TryGetProperty("content", out var contentArr))
                {
                    foreach (var item in contentArr.EnumerateArray())
                    {
                        var claim = new ExternalClaimDto
                        {
                            PlatformCode = PlatformCode,
                            PlatformClaimId = item.TryGetProperty("id", out var cid) ? cid.GetInt64().ToString() : "",
                            OrderNumber = item.TryGetProperty("orderNumber", out var on) ? on.GetString() ?? "" : "",
                            Status = item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
                            Reason = item.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "",
                            ReasonDetail = item.TryGetProperty("reasonDetail", out var rd) ? rd.GetString() : null,
                            CustomerName = item.TryGetProperty("customerFirstName", out var fn) ? fn.GetString() ?? "" : "",
                            ClaimDate = item.TryGetProperty("claimDate", out var cd) ? DateTimeOffset.FromUnixTimeMilliseconds(cd.GetInt64()).UtcDateTime : DateTime.UtcNow
                        };

                        if (item.TryGetProperty("items", out var claimItems))
                        {
                            foreach (var ci in claimItems.EnumerateArray())
                            {
                                claim.Lines.Add(new ExternalClaimLineDto
                                {
                                    Barcode = ci.TryGetProperty("barcode", out var bc) ? bc.GetString() : null,
                                    ProductName = ci.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "",
                                    Quantity = ci.TryGetProperty("quantity", out var qty) ? qty.GetInt32() : 1,
                                    UnitPrice = ci.TryGetProperty("price", out var up) ? up.GetDecimal() : 0
                                });
                            }
                        }

                        claims.Add(claim);
                    }
                }

                page++;
                hasMore = page < totalPages;
            }

            _logger.LogInformation("Trendyol PullClaims: {Count} claims retrieved", claims.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol PullClaims failed at page {Page}", page);
        }

        return claims.AsReadOnly();
    }

    public async Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.ApproveClaimAsync: ClaimId={ClaimId}", claimId);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent("{}", Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/order/sellers/{_supplierId}/claims/{claimId}/approve", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol ApproveClaim failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol ApproveClaim exception: {ClaimId}", claimId);
            return false;
        }
    }

    public async Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.RejectClaimAsync: ClaimId={ClaimId}", claimId);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new { claimIssueReasonId = reason };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/order/sellers/{_supplierId}/claims/{claimId}/issue", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol RejectClaim failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol RejectClaim exception: {ClaimId}", claimId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // ISettlementCapableAdapter — Muhasebe & Finans
    // ═══════════════════════════════════════════

    public async Task<SettlementDto?> GetSettlementAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetSettlementAsync: {Start} - {End}", startDate, endDate);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var startEpoch = new DateTimeOffset(startDate).ToUnixTimeMilliseconds();
            var endEpoch = new DateTimeOffset(endDate).ToUnixTimeMilliseconds();

            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(
                    new Uri($"/integration/finance/sellers/{_supplierId}/settlement?startDate={startEpoch}&endDate={endEpoch}", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol GetSettlement failed: {Status} - {Error}", response.StatusCode, error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var settlement = new SettlementDto
            {
                PlatformCode = PlatformCode,
                StartDate = startDate,
                EndDate = endDate
            };

            if (doc.RootElement.TryGetProperty("totalSales", out var ts)) settlement.TotalSales = ts.GetDecimal();
            if (doc.RootElement.TryGetProperty("totalCommission", out var tc)) settlement.TotalCommission = tc.GetDecimal();
            if (doc.RootElement.TryGetProperty("totalShippingCost", out var tsc)) settlement.TotalShippingCost = tsc.GetDecimal();
            if (doc.RootElement.TryGetProperty("totalReturnDeduction", out var trd)) settlement.TotalReturnDeduction = trd.GetDecimal();
            if (doc.RootElement.TryGetProperty("netAmount", out var na)) settlement.NetAmount = na.GetDecimal();

            _logger.LogInformation("Trendyol GetSettlement: Net={NetAmount} TRY", settlement.NetAmount);
            return settlement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol GetSettlement exception");
            return null;
        }
    }

    public async Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(DateTime startDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetCargoInvoicesAsync since={StartDate}", startDate);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var startEpoch = new DateTimeOffset(startDate).ToUnixTimeMilliseconds();

            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(
                    new Uri($"/integration/finance/sellers/{_supplierId}/cargo-invoices?startDate={startEpoch}", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol GetCargoInvoices failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<CargoInvoiceDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var invoices = JsonSerializer.Deserialize<List<CargoInvoiceDto>>(content, _jsonOptions) ?? new();

            _logger.LogInformation("Trendyol GetCargoInvoices: {Count} invoices", invoices.Count);
            return invoices.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol GetCargoInvoices exception");
            return Array.Empty<CargoInvoiceDto>();
        }
    }

    // ═══════════════════════════════════════════
    // Katalog Servisleri (Ek metotlar)
    // ═══════════════════════════════════════════

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetCategoriesAsync");

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(
                    new Uri("/integration/product/product-categories", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return Array.Empty<CategoryDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var categories = new List<CategoryDto>();
            if (doc.RootElement.TryGetProperty("categories", out var cats))
            {
                foreach (var cat in cats.EnumerateArray())
                {
                    categories.Add(ParseCategory(cat));
                }
            }

            _logger.LogInformation("Trendyol GetCategories: {Count} top-level categories", categories.Count);
            return categories.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol GetCategories exception");
            return Array.Empty<CategoryDto>();
        }
    }

    /// <summary>
    /// Kategori ozelliklerini (attributes) getirir — urun olusturmada zorunlu alanlar.
    /// Trendyol V2 endpoint: GET /integration/product/product-categories/{categoryId}/attributes
    /// </summary>
    public async Task<IReadOnlyList<CategoryAttributeDto>> GetCategoryAttributesAsync(int categoryId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetCategoryAttributesAsync categoryId={CategoryId}", categoryId);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(
                    new Uri($"/integration/product/product-categories/{categoryId}/attributes", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol GetCategoryAttributes failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<CategoryAttributeDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var attributes = new List<CategoryAttributeDto>();
            if (doc.RootElement.TryGetProperty("categoryAttributes", out var attrs))
            {
                foreach (var attr in attrs.EnumerateArray())
                {
                    var dto = new CategoryAttributeDto
                    {
                        AttributeId = attr.TryGetProperty("attribute", out var a) && a.TryGetProperty("id", out var aid) ? aid.GetInt32() : 0,
                        Name = attr.TryGetProperty("attribute", out var a2) && a2.TryGetProperty("name", out var aName) ? aName.GetString() ?? "" : "",
                        Required = attr.TryGetProperty("required", out var req) && req.GetBoolean(),
                        AllowCustom = attr.TryGetProperty("allowCustom", out var ac) && ac.GetBoolean(),
                        VariantType = attr.TryGetProperty("variantType", out var vt) ? vt.GetString() : null
                    };

                    if (attr.TryGetProperty("attributeValues", out var vals))
                    {
                        foreach (var val in vals.EnumerateArray())
                        {
                            dto.Values.Add(new CategoryAttributeValueDto
                            {
                                Id = val.TryGetProperty("id", out var vid) ? vid.GetInt32() : 0,
                                Name = val.TryGetProperty("name", out var vName) ? vName.GetString() ?? "" : ""
                            });
                        }
                    }

                    attributes.Add(dto);
                }
            }

            _logger.LogInformation("Trendyol GetCategoryAttributes: {Count} attributes for category {CategoryId}", attributes.Count, categoryId);
            return attributes.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol GetCategoryAttributes exception for category {CategoryId}", categoryId);
            return Array.Empty<CategoryAttributeDto>();
        }
    }

    /// <summary>
    /// Batch islem sonucunu sorgular — async product/inventory islemleri icin zorunlu.
    /// Trendyol endpoint: GET /integration/product/sellers/{sellerId}/products/batch-requests/{batchRequestId}
    /// </summary>
    public async Task<BatchRequestResultDto?> GetBatchRequestResultAsync(string batchRequestId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetBatchRequestResultAsync batchId={BatchId}", batchRequestId);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(
                    new Uri($"/integration/product/sellers/{_supplierId}/products/batch-requests/{batchRequestId}", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol GetBatchRequestResult failed: {Status} - {Error}", response.StatusCode, error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var result = new BatchRequestResultDto
            {
                BatchRequestId = batchRequestId,
                Status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
                ItemCount = root.TryGetProperty("itemCount", out var ic) ? ic.GetInt32() : 0,
                FailedItemCount = root.TryGetProperty("failedItemCount", out var fic) ? fic.GetInt32() : 0,
                CreationDate = root.TryGetProperty("creationDate", out var cd) ? DateTimeOffset.FromUnixTimeMilliseconds(cd.GetInt64()).UtcDateTime : null,
                LastModification = root.TryGetProperty("lastModification", out var lm) ? DateTimeOffset.FromUnixTimeMilliseconds(lm.GetInt64()).UtcDateTime : null
            };

            if (root.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var batchItem = new BatchItemDto
                    {
                        RequestItem = item.TryGetProperty("requestItem", out var ri) ? ri.GetRawText() : null,
                        Status = item.TryGetProperty("status", out var itemSt) ? itemSt.GetString() ?? "" : ""
                    };

                    if (item.TryGetProperty("failureReasons", out var reasons))
                    {
                        foreach (var reason in reasons.EnumerateArray())
                        {
                            batchItem.FailureReasons.Add(reason.GetString() ?? "");
                        }
                    }

                    result.Items.Add(batchItem);
                }
            }

            _logger.LogInformation("Trendyol GetBatchRequestResult: BatchId={BatchId} Status={Status} Failed={Failed}/{Total}",
                batchRequestId, result.Status, result.FailedItemCount, result.ItemCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol GetBatchRequestResult exception for batchId {BatchId}", batchRequestId);
            return null;
        }
    }

    public async Task<IReadOnlyList<BrandDto>> GetBrandsAsync(string namePrefix, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetBrandsAsync prefix={Prefix}", namePrefix);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(
                    new Uri($"/integration/product/brands?name={Uri.EscapeDataString(namePrefix)}", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return Array.Empty<BrandDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var brands = new List<BrandDto>();
            if (doc.RootElement.TryGetProperty("brands", out var brandArr))
            {
                foreach (var b in brandArr.EnumerateArray())
                {
                    brands.Add(new BrandDto
                    {
                        PlatformBrandId = b.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                        Name = b.TryGetProperty("name", out var n) ? n.GetString() ?? "" : ""
                    });
                }
            }

            return brands.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol GetBrands exception");
            return Array.Empty<BrandDto>();
        }
    }

    public async Task<PlatformHealthDto> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new PlatformHealthDto { PlatformCode = PlatformCode };

        try
        {
            var response = await _httpClient.GetAsync(
                new Uri("/integration/product/api-status", UriKind.Relative), ct).ConfigureAwait(false);

            sw.Stop();
            result.LatencyMs = (int)sw.ElapsedMilliseconds;
            result.IsHealthy = response.IsSuccessStatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {body[..Math.Min(body.Length, 200)]}";
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.LatencyMs = (int)sw.ElapsedMilliseconds;
            result.IsHealthy = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    // ═══════════════════════════════════════════
    // IWebhookCapableAdapter
    // ═══════════════════════════════════════════

    public async Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.RegisterWebhookAsync: {Url}", callbackUrl);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new { url = callbackUrl };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/order/sellers/{_supplierId}/webhooks", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol RegisterWebhook exception");
            return false;
        }
    }

    public async Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.UnregisterWebhookAsync");
        await Task.CompletedTask.ConfigureAwait(false);
        return true;
    }

    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(payload);
        var eventType = doc.RootElement.TryGetProperty("eventType", out var et) ? et.GetString() : "unknown";
        var orderId = doc.RootElement.TryGetProperty("orderNumber", out var on) ? on.GetString() : null;

        _logger.LogInformation(
            "TrendyolAdapter webhook processed: EventType={EventType} OrderId={OrderId} PayloadLength={Length}",
            eventType, orderId, payload.Length);

        return Task.CompletedTask;
    }

    // ═══════════════════════════════════════════
    // Extended Product Operations
    // ═══════════════════════════════════════════

    /// <summary>
    /// Urunleri arsivler — barcode listesi ile toplu arsivleme.
    /// POST /v2/{supplierId}/products/archive
    /// </summary>
    public async Task<bool> ArchiveProductsAsync(List<string> barcodes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(barcodes);
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.ArchiveProductsAsync: {Count} barcodes", barcodes.Count);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new { items = barcodes.Select(b => new { barcode = b }).ToArray() };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/product/sellers/{_supplierId}/v2/products/archive", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol ArchiveProducts failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol ArchiveProducts success: {Count} products archived", barcodes.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol ArchiveProducts exception");
            return false;
        }
    }

    /// <summary>
    /// Arsivlenmis urunleri tekrar aktif eder.
    /// POST /v2/{supplierId}/products/unlock
    /// </summary>
    public async Task<bool> UnlockProductsAsync(List<string> barcodes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(barcodes);
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.UnlockProductsAsync: {Count} barcodes", barcodes.Count);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new { items = barcodes.Select(b => new { barcode = b }).ToArray() };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/product/sellers/{_supplierId}/v2/products/unlock", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol UnlockProducts failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol UnlockProducts success: {Count} products unlocked", barcodes.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol UnlockProducts exception");
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // Musteri Sorulari (Q&A)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Musteri sorularini sayfalanmis olarak getirir.
    /// GET /v2/{supplierId}/questions?page={page}&amp;size={size}
    /// </summary>
    public async Task<IReadOnlyList<TrendyolCustomerQuestion>> GetQuestionsAsync(int page, int size, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetQuestionsAsync page={Page} size={Size}", page, size);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(
                    new Uri($"/integration/product/sellers/{_supplierId}/questions?page={page}&size={size}", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol GetQuestions failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<TrendyolCustomerQuestion>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var questions = new List<TrendyolCustomerQuestion>();
            if (doc.RootElement.TryGetProperty("content", out var contentArr))
            {
                foreach (var item in contentArr.EnumerateArray())
                {
                    questions.Add(new TrendyolCustomerQuestion(
                        Id: item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                        Text: item.TryGetProperty("text", out var text) ? text.GetString() ?? "" : "",
                        ProductId: item.TryGetProperty("productId", out var pid) ? pid.GetInt64() : 0,
                        Status: item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
                        CreatedAt: item.TryGetProperty("creationDate", out var cd)
                            ? DateTimeOffset.FromUnixTimeMilliseconds(cd.GetInt64()).UtcDateTime
                            : DateTime.UtcNow
                    ));
                }
            }

            _logger.LogInformation("Trendyol GetQuestions: {Count} questions retrieved", questions.Count);
            return questions.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol GetQuestions exception");
            return Array.Empty<TrendyolCustomerQuestion>();
        }
    }

    /// <summary>
    /// Musteri sorusunu yanitlar.
    /// POST /v2/{supplierId}/questions/{questionId}/answers
    /// </summary>
    public async Task<bool> AnswerQuestionAsync(long questionId, string answerText, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.AnswerQuestionAsync: QuestionId={QuestionId}", questionId);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new { text = answerText };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/product/sellers/{_supplierId}/questions/{questionId}/answers", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol AnswerQuestion failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol AnswerQuestion success: QuestionId={QuestionId}", questionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol AnswerQuestion exception: {QuestionId}", questionId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // Extended Claim Operations
    // ═══════════════════════════════════════════

    /// <summary>
    /// Iade taleplerini tarih araligiyla getirir.
    /// GET /v2/{supplierId}/claims?startDate=X&amp;endDate=Y
    /// </summary>
    public async Task<IReadOnlyList<TrendyolClaimDto>> GetClaimsAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetClaimsAsync: {Start} - {End}", startDate, endDate);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var url = $"/integration/order/sellers/{_supplierId}/claims?page=0&size=50";
            if (startDate.HasValue)
            {
                var epoch = new DateTimeOffset(startDate.Value).ToUnixTimeMilliseconds();
                url += $"&startDate={epoch}";
            }
            if (endDate.HasValue)
            {
                var epoch = new DateTimeOffset(endDate.Value).ToUnixTimeMilliseconds();
                url += $"&endDate={epoch}";
            }

            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(
                    new Uri(url, UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol GetClaims failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<TrendyolClaimDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var claims = new List<TrendyolClaimDto>();
            if (doc.RootElement.TryGetProperty("content", out var contentArr))
            {
                foreach (var item in contentArr.EnumerateArray())
                {
                    claims.Add(new TrendyolClaimDto(
                        Id: item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                        OrderId: item.TryGetProperty("orderId", out var oid) ? oid.GetInt64() : 0,
                        Reason: item.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "",
                        Status: item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
                        Amount: item.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0
                    ));
                }
            }

            _logger.LogInformation("Trendyol GetClaims: {Count} claims retrieved", claims.Count);
            return claims.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol GetClaims exception");
            return Array.Empty<TrendyolClaimDto>();
        }
    }

    /// <summary>
    /// Iade talebini onaylar (long claimId versiyonu).
    /// PUT /v2/{supplierId}/claims/{claimId}/approve
    /// </summary>
    public async Task<bool> ApproveClaimByIdAsync(long claimId, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.ApproveClaimByIdAsync: ClaimId={ClaimId}", claimId);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent("{}", Encoding.UTF8, "application/json");
                    return await _httpClient.PutAsync(
                        new Uri($"/integration/order/sellers/{_supplierId}/claims/{claimId}/approve", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol ApproveClaimById failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol ApproveClaimById success: ClaimId={ClaimId}", claimId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol ApproveClaimById exception: {ClaimId}", claimId);
            return false;
        }
    }

    /// <summary>
    /// Iade talebini reddeder (long claimId + rejectReason).
    /// PUT /v2/{supplierId}/claims/{claimId}/reject
    /// </summary>
    public async Task<bool> RejectClaimByIdAsync(long claimId, string rejectReason, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.RejectClaimByIdAsync: ClaimId={ClaimId}", claimId);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new { rejectReason };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PutAsync(
                        new Uri($"/integration/order/sellers/{_supplierId}/claims/{claimId}/reject", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol RejectClaimById failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol RejectClaimById success: ClaimId={ClaimId}", claimId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol RejectClaimById exception: {ClaimId}", claimId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // Fatura Gonderme (Extended)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Siparis icin fatura bilgisi gonderir.
    /// POST /v2/{supplierId}/invoices
    /// </summary>
    public async Task<bool> SendInvoiceAsync(long orderId, string invoiceNumber, DateTime invoiceDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.SendInvoiceAsync: OrderId={OrderId} Invoice={InvoiceNumber}", orderId, invoiceNumber);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                orderId,
                invoiceNumber,
                invoiceDate = invoiceDate.ToString("yyyy-MM-dd")
            };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/order/sellers/{_supplierId}/invoices", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol SendInvoice failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol SendInvoice success: OrderId={OrderId}", orderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol SendInvoice exception: {OrderId}", orderId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // Muhasebe & Finans (Extended)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Hesap ekstre satirlarini tarih araligiyla getirir.
    /// GET /v2/{supplierId}/settlements?startDate=X&amp;endDate=Y
    /// </summary>
    public async Task<IReadOnlyList<TrendyolSettlementItemDto>> GetSettlementsAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetSettlementsAsync: {Start} - {End}", startDate, endDate);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var startEpoch = new DateTimeOffset(startDate).ToUnixTimeMilliseconds();
            var endEpoch = new DateTimeOffset(endDate).ToUnixTimeMilliseconds();

            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(
                    new Uri($"/integration/finance/sellers/{_supplierId}/settlements?startDate={startEpoch}&endDate={endEpoch}", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol GetSettlements failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<TrendyolSettlementItemDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var settlements = new List<TrendyolSettlementItemDto>();
            if (doc.RootElement.TryGetProperty("content", out var contentArr))
            {
                foreach (var item in contentArr.EnumerateArray())
                {
                    settlements.Add(new TrendyolSettlementItemDto(
                        Id: item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                        Amount: item.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0,
                        Currency: item.TryGetProperty("currency", out var cur) ? cur.GetString() ?? "TRY" : "TRY",
                        Status: item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
                        Date: item.TryGetProperty("date", out var dt)
                            ? DateTimeOffset.FromUnixTimeMilliseconds(dt.GetInt64()).UtcDateTime
                            : DateTime.UtcNow
                    ));
                }
            }

            _logger.LogInformation("Trendyol GetSettlements: {Count} settlement items retrieved", settlements.Count);
            return settlements.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol GetSettlements exception");
            return Array.Empty<TrendyolSettlementItemDto>();
        }
    }

    // ═══════════════════════════════════════════
    // Paket Yonetimi (Package Operations)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Paketi boler — belirli siparis satirlarini yeni bir pakete ayirir.
    /// POST /v2/{supplierId}/packages/{packageId}/split
    /// </summary>
    public async Task<bool> SplitPackageAsync(long packageId, List<long> orderLineIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(orderLineIds);
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.SplitPackageAsync: PackageId={PackageId} Lines={Count}", packageId, orderLineIds.Count);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new { orderLineIds };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/integration/order/sellers/{_supplierId}/packages/{packageId}/split", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol SplitPackage failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol SplitPackage success: PackageId={PackageId}", packageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol SplitPackage exception: {PackageId}", packageId);
            return false;
        }
    }

    /// <summary>
    /// Paket kutu/desi bilgisini gunceller.
    /// PUT /v2/{supplierId}/packages/{packageId}/box-info
    /// </summary>
    public async Task<bool> UpdateBoxInfoAsync(long packageId, int desi, int boxCount, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.UpdateBoxInfoAsync: PackageId={PackageId} Desi={Desi} BoxCount={BoxCount}", packageId, desi, boxCount);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new { desi, boxCount };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PutAsync(
                        new Uri($"/integration/order/sellers/{_supplierId}/packages/{packageId}/box-info", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol UpdateBoxInfo failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol UpdateBoxInfo success: PackageId={PackageId}", packageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol UpdateBoxInfo exception: {PackageId}", packageId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // Tazminat Sorgusu (Compensations)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Tazminat bilgilerini getirir.
    /// GET /v2/{supplierId}/claims/compensation
    /// </summary>
    public async Task<IReadOnlyList<TrendyolCompensationDto>> GetCompensationsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetCompensationsAsync");

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var response = await _retryPipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync(
                    new Uri($"/integration/order/sellers/{_supplierId}/claims/compensation", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol GetCompensations failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<TrendyolCompensationDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var compensations = new List<TrendyolCompensationDto>();
            if (doc.RootElement.TryGetProperty("content", out var contentArr))
            {
                foreach (var item in contentArr.EnumerateArray())
                {
                    compensations.Add(new TrendyolCompensationDto(
                        Id: item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                        ClaimId: item.TryGetProperty("claimId", out var cid) ? cid.GetInt64() : 0,
                        Amount: item.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0,
                        Status: item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : ""
                    ));
                }
            }

            _logger.LogInformation("Trendyol GetCompensations: {Count} compensations retrieved", compensations.Count);
            return compensations.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol GetCompensations exception");
            return Array.Empty<TrendyolCompensationDto>();
        }
    }

    // ═══════════════════════════════════════════
    // Yardimci Metotlar
    // ═══════════════════════════════════════════

    private static string BuildTrendyolCustomerName(JsonElement item)
    {
        var first = item.TryGetProperty("customerFirstName", out var fn) ? fn.GetString() ?? "" : "";
        var last = item.TryGetProperty("customerLastName", out var ln) ? ln.GetString() ?? "" : "";
        return $"{first} {last}".Trim();
    }

    private static CategoryDto ParseCategory(JsonElement el)
    {
        var cat = new CategoryDto
        {
            PlatformCategoryId = el.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
            Name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
            ParentId = el.TryGetProperty("parentId", out var pid) ? pid.GetInt32() : null
        };

        if (el.TryGetProperty("subCategories", out var subs))
        {
            foreach (var sub in subs.EnumerateArray())
                cat.SubCategories.Add(ParseCategory(sub));
        }

        return cat;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "TrendyolAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
    }

    private async Task ApplyRateLimitAsync(CancellationToken ct = default)
    {
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Mevcut TrendyolApiClient'tan alinan rate limiting mantigi
            await Task.Delay(10, ct).ConfigureAwait(false); // min 10ms between requests
        }
        finally
        {
            _rateLimitSemaphore.Release();
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

            // HEAD request to Trendyol API root — no auth needed, any HTTP response = reachable
            var request = new HttpRequestMessage(HttpMethod.Head,
                new Uri(_options.BaseUrl, UriKind.Absolute));
            using var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);

            _logger.LogDebug("Trendyol ping: {StatusCode}", response.StatusCode);
            return true; // Any response means the host is reachable
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Trendyol ping failed");
            return false;
        }
    }
}
