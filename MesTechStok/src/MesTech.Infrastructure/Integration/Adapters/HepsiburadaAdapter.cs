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
/// Hepsiburada platform adaptoru — Dalga 3 tam entegrasyon.
/// IIntegratorAdapter + IOrderCapableAdapter + IShipmentCapableAdapter
/// K1c-03: OAuth token auth (HepsiburadaTokenService), Polly retry + 401 token refresh, SemaphoreSlim rate limiting.
/// </summary>
public class HepsiburadaAdapter : IIntegratorAdapter, IOrderCapableAdapter, IShipmentCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly HepsiburadaTokenService? _tokenService;
    private readonly ILogger<HepsiburadaAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(20, 20);

    private string _merchantId = string.Empty;
    private bool _isConfigured;

    public HepsiburadaAdapter(HttpClient httpClient, ILogger<HepsiburadaAdapter> logger,
        HepsiburadaTokenService? tokenService = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenService = tokenService;

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
                        "Hepsiburada API retry {Attempt} after {Delay}ms",
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

    public string PlatformCode => nameof(PlatformType.Hepsiburada);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ── Auth ────────────────────────────────────────────
    private void ConfigureAuth(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        _merchantId = credentials.GetValueOrDefault("MerchantId", "");

        // K1c-03: OAuth token replaces static Bearer MerchantId:ApiKey header.
        // Token will be set per-request in ApplyAuthHeaderAsync.
        // Fall back to legacy static header only when token service is unavailable.
        if (_tokenService is null)
        {
            var apiKey = credentials.GetValueOrDefault("ApiKey", "");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", $"{_merchantId}:{apiKey}");
            _logger.LogWarning("HepsiburadaTokenService not available — falling back to static Bearer header");
        }

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MesTech-Hepsiburada-Client/3.0");

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _httpClient.BaseAddress = new Uri(credentials["BaseUrl"], UriKind.Absolute);

        _isConfigured = true;
    }

    /// <summary>
    /// K1c-03: Apply OAuth token to request. Uses HepsiburadaTokenService when available.
    /// </summary>
    private async Task ApplyAuthHeaderAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (_tokenService is not null)
        {
            var token = await _tokenService.GetAccessTokenAsync(ct).ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "HepsiburadaAdapter henuz konfigure edilmedi. Once TestConnectionAsync cagirin.");
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
            if (!credentials.ContainsKey("MerchantId") || string.IsNullOrWhiteSpace(credentials["MerchantId"]))
            {
                result.ErrorMessage = "MerchantId alani zorunludur.";
                result.ResponseTime = sw.Elapsed;
                return result;
            }

            if (!credentials.ContainsKey("ApiKey") || string.IsNullOrWhiteSpace(credentials["ApiKey"]))
            {
                result.ErrorMessage = "ApiKey alani zorunludur.";
                result.ResponseTime = sw.Elapsed;
                return result;
            }

            ConfigureAuth(credentials);

            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get,
                    $"/listings/merchantid/{_merchantId}?limit=1&offset=0"), ct);

            result.HttpStatusCode = (int)response.StatusCode;
            sw.Stop();
            result.ResponseTime = sw.Elapsed;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var hbResponse = JsonSerializer.Deserialize<HbListingsResponse>(content, _jsonOptions);

                result.IsSuccess = true;
                result.ProductCount = hbResponse?.TotalCount ?? 0;
                result.StoreName = "Hepsiburada Magaza";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Yetkisiz erisim — MerchantId veya ApiKey hatali.",
                    System.Net.HttpStatusCode.Forbidden => "Erisim engellendi — API Key yetkisiz.",
                    _ => $"Hepsiburada API hatasi: {response.StatusCode} - {errorContent}"
                };
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            result.ErrorMessage = $"Baglanti hatasi: {ex.Message}";
            result.ResponseTime = sw.Elapsed;
            _logger.LogError(ex, "Hepsiburada TestConnection failed");
        }

        return result;
    }

    // ── Product methods ─────────────────────────────────
    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        _logger.LogWarning("Hepsiburada PushProduct not supported — HB API does not allow product creation");
        return Task.FromResult(false);
    }

    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        var products = new List<Product>();
        var offset = 0;
        const int limit = 50;

        while (!ct.IsCancellationRequested)
        {
            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get,
                    $"/listings/merchantid/{_merchantId}?limit={limit}&offset={offset}"), ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Hepsiburada PullProducts offset {Offset} failed: {Status}",
                    offset, response.StatusCode);
                break;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var hbResponse = JsonSerializer.Deserialize<HbListingsResponse>(content, _jsonOptions);

            if (hbResponse?.Listings is null || hbResponse.Listings.Count == 0)
                break;

            foreach (var listing in hbResponse.Listings)
            {
                // Skip Banned listings
                if (string.Equals(listing.ListingStatus, "Banned", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Hepsiburada listing skipped (Banned): {Sku} - {Name}",
                        listing.MerchantSku, listing.ProductName);
                    continue;
                }

                // Passive → IsActive=false, Active → IsActive=true
                var isActive = string.Equals(listing.ListingStatus, "Active", StringComparison.OrdinalIgnoreCase);

                products.Add(new Product
                {
                    Name = listing.ProductName,
                    SKU = listing.MerchantSku,
                    Barcode = listing.Barcode,
                    Description = listing.Description,
                    SalePrice = listing.Price,
                    Stock = listing.AvailableStock,
                    IsActive = isActive
                });
            }

            offset += limit;

            if (offset >= hbResponse.TotalCount)
                break;
        }

        _logger.LogInformation("Hepsiburada PullProducts: {Count} products fetched", products.Count);
        return products.AsReadOnly();
    }

    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();

        var payload = new
        {
            listings = new[] { new { merchantSku = productId.ToString(), availableStock = newStock } }
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/listings/and-inventory");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Hepsiburada stock update OK: {ProductId} → {Stock}", productId, newStock);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Hepsiburada stock update failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();

        var payload = new
        {
            listings = new[] { new { merchantSku = productId.ToString(), price = newPrice } }
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/listings/and-inventory");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Hepsiburada price update OK: {ProductId} → {Price}", productId, newPrice);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Hepsiburada price update failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Order methods ───────────────────────────────────
    public async Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(DateTime? since = null, CancellationToken ct = default)
    {
        EnsureConfigured();
        var orders = new List<ExternalOrderDto>();
        var offset = 0;
        const int limit = 50;

        var baseUrl = $"/orders/merchantid/{_merchantId}?limit={limit}";
        if (since.HasValue)
            baseUrl += "&startDate=" + since.Value.ToString("yyyy-MM-dd");

        while (!ct.IsCancellationRequested)
        {
            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}&offset={offset}"), ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Hepsiburada PullOrders offset {Offset} failed: {Status}",
                    offset, response.StatusCode);
                break;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var hbResponse = JsonSerializer.Deserialize<HbOrderListResponse>(content, _jsonOptions);

            if (hbResponse?.Orders is null || hbResponse.Orders.Count == 0)
                break;

            foreach (var hbOrder in hbResponse.Orders)
            {
                // Package-based: no sub-order flattening — each order maps to one ExternalOrderDto
                orders.Add(new ExternalOrderDto
                {
                    PlatformOrderId = hbOrder.OrderNumber,
                    PlatformCode = PlatformCode,
                    OrderNumber = hbOrder.OrderNumber,
                    Status = hbOrder.Status,
                    CustomerName = hbOrder.CustomerName,
                    CustomerEmail = hbOrder.CustomerEmail,
                    CustomerAddress = hbOrder.DeliveryAddress,
                    CustomerCity = hbOrder.DeliveryCity,
                    TotalAmount = hbOrder.TotalAmount,
                    ShipmentPackageId = hbOrder.PackageNumber,
                    OrderDate = hbOrder.OrderDate,
                    Lines = hbOrder.Lines.Select(item => new ExternalOrderLineDto
                    {
                        PlatformLineId = item.HepsiburadaSku,
                        SKU = item.MerchantSku,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        LineTotal = item.TotalPrice
                    }).ToList()
                });
            }

            offset += limit;

            if (offset >= hbResponse.TotalCount)
                break;
        }

        _logger.LogInformation("Hepsiburada PullOrders: {Count} orders fetched", orders.Count);
        return orders.AsReadOnly();
    }

    public async Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default)
    {
        EnsureConfigured();

        var payload = new { status };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Put, $"/packages/{packageId}/status");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Hepsiburada order status updated: {PackageId} → {Status}", packageId, status);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Hepsiburada order status update failed {Status}: {Error}", response.StatusCode, error);
        return false;
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
            CargoProvider.MngKargo => "MNG Kargo",
            CargoProvider.PttKargo => "PTT Kargo",
            CargoProvider.Hepsijet => "HepsiJet",
            CargoProvider.UPS => "UPS",
            _ => provider.ToString()
        };

        var payload = new
        {
            cargoCompany,
            trackingNumber
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"/packages/{platformOrderId}/shipment");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Hepsiburada shipment sent: Package={PackageId} Tracking={Tracking}",
                platformOrderId, trackingNumber);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Hepsiburada shipment failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Claims ───────────────────────────────────────────

    /// <summary>
    /// GET /claims — Hepsiburada iade/claim listesini ceker.
    /// </summary>
    public async Task<List<HbClaimDto>> GetClaimsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/claims"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Hepsiburada GetClaims failed {Status}: {Error}", response.StatusCode, error);
            return new List<HbClaimDto>();
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var hbResponse = JsonSerializer.Deserialize<HbClaimListResponse>(content, _jsonOptions);

        _logger.LogInformation("Hepsiburada GetClaims: {Count} claims fetched", hbResponse?.Claims.Count ?? 0);
        return hbResponse?.Claims ?? new List<HbClaimDto>();
    }

    /// <summary>
    /// POST /claims/{claimId}/approve — Claim'i onaylar.
    /// </summary>
    public async Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Post, $"/claims/{claimId}/approve"), ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Hepsiburada claim approved: {ClaimId}", claimId);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Hepsiburada claim approval failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    /// <summary>
    /// POST /claims/{claimId}/reject — Claim'i reddeder.
    /// </summary>
    public async Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default)
    {
        EnsureConfigured();

        var payload = new { reason };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"/claims/{claimId}/reject");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Hepsiburada claim rejected: {ClaimId} Reason={Reason}", claimId, reason);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Hepsiburada claim rejection failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Listing Activation ──────────────────────────────

    /// <summary>
    /// PUT /listings/{sku}/activate — Listing'i aktif eder.
    /// </summary>
    public async Task<bool> ActivateListingAsync(string sku, CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Put, $"/listings/{sku}/activate"), ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Hepsiburada listing activated: {Sku}", sku);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Hepsiburada listing activation failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    /// <summary>
    /// PUT /listings/{sku}/deactivate — Listing'i pasif eder.
    /// </summary>
    public async Task<bool> DeactivateListingAsync(string sku, CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Put, $"/listings/{sku}/deactivate"), ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Hepsiburada listing deactivated: {Sku}", sku);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Hepsiburada listing deactivation failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Upload Status ───────────────────────────────────

    /// <summary>
    /// GET /listings/upload-status/{correlationId} — Yukleme durumunu sorgular.
    /// </summary>
    public async Task<HbUploadStatusDto?> CheckUploadStatusAsync(string correlationId, CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/listings/upload-status/{correlationId}"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Hepsiburada CheckUploadStatus failed {Status}: {Error}", response.StatusCode, error);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var status = JsonSerializer.Deserialize<HbUploadStatusDto>(content, _jsonOptions);

        _logger.LogInformation("Hepsiburada upload status: {CorrelationId} → {Status}",
            correlationId, status?.Status);
        return status;
    }

    // ── Commissions ─────────────────────────────────────

    /// <summary>
    /// GET /finance/commissions — Komisyon bilgilerini ceker.
    /// </summary>
    public async Task<List<HbCommissionDto>> GetCommissionsAsync(DateTime start, DateTime end, CancellationToken ct = default)
    {
        EnsureConfigured();

        var url = $"/finance/commissions?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}";

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, url), ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Hepsiburada GetCommissions failed {Status}: {Error}", response.StatusCode, error);
            return new List<HbCommissionDto>();
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var hbResponse = JsonSerializer.Deserialize<HbCommissionListResponse>(content, _jsonOptions);

        _logger.LogInformation("Hepsiburada GetCommissions: {Count} records fetched",
            hbResponse?.Commissions.Count ?? 0);
        return hbResponse?.Commissions ?? new List<HbCommissionDto>();
    }

    // ── Invoice ─────────────────────────────────────────

    /// <summary>
    /// POST /invoices — Fatura bilgisi gonderir.
    /// </summary>
    public async Task<bool> SendInvoiceAsync(string orderId, string invoiceNo, DateTime date, CancellationToken ct = default)
    {
        EnsureConfigured();

        var payload = new
        {
            orderId,
            invoiceNumber = invoiceNo,
            invoiceDate = date.ToString("yyyy-MM-dd")
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);

        var response = await ExecuteWithRetryAsync(
            () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/invoices");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Hepsiburada invoice sent: Order={OrderId} Invoice={InvoiceNo}",
                orderId, invoiceNo);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Hepsiburada invoice send failed {Status}: {Error}", response.StatusCode, error);
        return false;
    }

    // ── Cargo Label ─────────────────────────────────────

    /// <summary>
    /// GET /packages/{packageId}/label — Kargo etiketi PDF indirir.
    /// </summary>
    public async Task<byte[]?> GetCargoLabelAsync(string packageId, CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/packages/{packageId}/label"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Hepsiburada GetCargoLabel failed {Status}: {Error}", response.StatusCode, error);
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Hepsiburada cargo label downloaded: {PackageId} ({Size} bytes)",
            packageId, bytes.Length);
        return bytes;
    }

    // ── Shipment Tracking ───────────────────────────────

    /// <summary>
    /// GET /transportation/tracking/{trackingNo} — Kargo takip bilgisi sorgular.
    /// </summary>
    public async Task<HbTrackingDto?> GetShipmentTrackingAsync(string trackingNo, CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/transportation/tracking/{trackingNo}"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Hepsiburada GetShipmentTracking failed {Status}: {Error}", response.StatusCode, error);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var tracking = JsonSerializer.Deserialize<HbTrackingDto>(content, _jsonOptions);

        _logger.LogInformation("Hepsiburada shipment tracking: {TrackingNo} → {Status}",
            trackingNo, tracking?.Status);
        return tracking;
    }

    // ── Categories ──────────────────────────────────────
    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());

    // ── HTTP helper ─────────────────────────────────────
    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var response = await _retryPipeline.ExecuteAsync(async token =>
            {
                using var request = requestFactory();
                await ApplyAuthHeaderAsync(request, token).ConfigureAwait(false);
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            // K1c-04: On 401 — invalidate token, refresh, retry once
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && _tokenService is not null)
            {
                _logger.LogWarning("{Platform} received 401 — refreshing OAuth token and retrying", PlatformCode);
                _tokenService.InvalidateToken();

                response = await _retryPipeline.ExecuteAsync(async token =>
                {
                    using var retryRequest = requestFactory();
                    await ApplyAuthHeaderAsync(retryRequest, token).ConfigureAwait(false);
                    return await _httpClient.SendAsync(retryRequest, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);
            }

            return response;
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
