using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
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
/// Bearer MerchantId:ApiKey auth, Polly retry, SemaphoreSlim rate limiting.
/// </summary>
public class HepsiburadaAdapter : IIntegratorAdapter, IOrderCapableAdapter, IShipmentCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HepsiburadaAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(20, 20);

    private string _merchantId = string.Empty;
    private bool _isConfigured;

    public HepsiburadaAdapter(HttpClient httpClient, ILogger<HepsiburadaAdapter> logger)
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
        var apiKey = credentials.GetValueOrDefault("ApiKey", "");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", $"{_merchantId}:{apiKey}");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MesTech-Hepsiburada-Client/3.0");

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _httpClient.BaseAddress = new Uri(credentials["BaseUrl"], UriKind.Absolute);

        _isConfigured = true;
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
            return await _retryPipeline.ExecuteAsync(async token =>
            {
                using var request = requestFactory();
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }
}
