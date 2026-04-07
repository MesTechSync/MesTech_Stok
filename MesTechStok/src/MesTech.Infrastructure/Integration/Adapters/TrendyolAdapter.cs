using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Security;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
public sealed class TrendyolAdapter : IIntegratorAdapter, IWebhookCapableAdapter,
    IOrderCapableAdapter, IInvoiceCapableAdapter, IClaimCapableAdapter, ISettlementCapableAdapter,
    IShipmentCapableAdapter, IPingableAdapter, IReviewCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TrendyolAdapter> _logger;
    private readonly TrendyolOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private readonly IServiceScopeFactory? _scopeFactory;

    // Trendyol API: 50 req/10s limit — 100 concurrency allows burst queueing with 5-attempt 429 retry.
    // Higher than other adapters (10-20) due to batch product sync volume (10K+ SKU).
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(100, 100);
    private volatile int _totalRequests;
    private volatile int _throttledRequests;
    private DateTime _lastThrottleAt;

    // Credential key'leri — StoreCredential tablosundaki Key alanlari
    private string? _supplierId;
    private AuthenticationHeaderValue? _authHeader;
    private bool _isConfigured;

    public TrendyolAdapter(HttpClient httpClient, ILogger<TrendyolAdapter> logger,
        IOptions<TrendyolOptions>? options = null, IConfiguration? configuration = null,
        IServiceScopeFactory? scopeFactory = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory;
        _options = options?.Value ?? new TrendyolOptions();
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);

        // Initialise BaseAddress from options so sandbox toggle works without credential override
        _httpClient.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);

        // Auto-configure from IConfiguration (user-secrets / appsettings / env vars) if credentials present
        if (configuration is not null)
        {
            var section = configuration.GetSection(TrendyolOptions.Section);
            var apiKey = section["ApiKey"];
            var apiSecret = section["ApiSecret"];
            var supplierId = section["SupplierId"];

            _logger.LogDebug("TrendyolAdapter auto-configure check: Enabled={Enabled}, ApiKey={HasKey}, SupplierId={SupplierId}",
                _options.Enabled, !string.IsNullOrEmpty(apiKey), supplierId ?? "(null)");

            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret) && !string.IsNullOrEmpty(supplierId))
            {
                ConfigureAuth(new Dictionary<string, string>
                {
                    ["ApiKey"] = apiKey,
                    ["ApiSecret"] = apiSecret,
                    ["SupplierId"] = supplierId
                });
                _logger.LogInformation("TrendyolAdapter auto-configured from IConfiguration: SupplierId={SupplierId}", supplierId);
            }
        }

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
                    Interlocked.Increment(ref _throttledRequests);
                    _lastThrottleAt = DateTime.UtcNow;
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

    /// <summary>Rate limit telemetry for connection test panel.</summary>
#pragma warning disable CA1024 // Telemetry snapshot — method semantics preferred over property
    public RateLimitInfo GetRateLimitInfo() => new(
        ConcurrentSlots: _rateLimitSemaphore.CurrentCount,
        MaxConcurrentSlots: 100,
        TotalRequests: _totalRequests,
        ThrottledRequests: _throttledRequests,
        LastThrottleAt: _lastThrottleAt == default ? null : _lastThrottleAt);
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
        _authHeader = new AuthenticationHeaderValue("Basic", encoded);

        // Support BaseUrl override for sandbox testing via credentials — SSRF guard (G106)
        var rawBaseUrl = credentials.GetValueOrDefault("BaseUrl", "");
        if (!string.IsNullOrEmpty(rawBaseUrl))
        {
            if (!Uri.TryCreate(rawBaseUrl, UriKind.Absolute, out var parsedUri) ||
                (parsedUri.Scheme != "https" && parsedUri.Scheme != "http"))
                throw new ArgumentException($"Invalid Trendyol base URL scheme: {rawBaseUrl}. Only HTTP(S) allowed.");
            if (SsrfGuard.IsPrivateHost(parsedUri.Host))
                _logger.LogWarning("[TrendyolAdapter] BaseUrl points to private network: {BaseUrl}", rawBaseUrl);
            _httpClient.BaseAddress = parsedUri;
        }

        // UseSandbox=true shortcut sets sandbox URL automatically
        if (credentials.GetValueOrDefault("UseSandbox", "false").Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            _httpClient.BaseAddress = new Uri(_options.SandboxBaseUrl, UriKind.Absolute);
        }

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
            using var testRequest = CreateAuthenticatedRequest(HttpMethod.Get,
                new Uri($"/integration/product/sellers/{_supplierId}/products?page=0&size=1", UriKind.Relative));
            using var response = await _httpClient.SendAsync(testRequest, ct).ConfigureAwait(false);

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
            var platformBrandId = 0;
            if (product.BrandEntity?.PlatformMappings is { Count: > 0 })
            {
                var trendyolBrandMapping = product.BrandEntity.PlatformMappings
                    .FirstOrDefault(m => m.PlatformType == PlatformType.Trendyol);
                if (trendyolBrandMapping is not null
                    && int.TryParse(trendyolBrandMapping.ExternalBrandId, out var parsedBrandId))
                {
                    platformBrandId = parsedBrandId;
                }
            }

            if (platformBrandId == 0)
            {
                _logger.LogError("Trendyol PushProduct ABORTED: brandId=0 for SKU={SKU}. " +
                    "Trendyol API requires a valid brandId. Map brand via BrandPlatformMapping.", product.SKU);
                return false;
            }

            // categoryId must be integer for Trendyol API — resolve from CategoryPlatformMapping
            var platformCategoryId = 0;
            if (product.PlatformMappings is { Count: > 0 })
            {
                var trendyolMapping = product.PlatformMappings
                    .FirstOrDefault(m => m.PlatformType == PlatformType.Trendyol);
                if (trendyolMapping?.ExternalCategoryId is not null
                    && int.TryParse(trendyolMapping.ExternalCategoryId, out var parsedCatId))
                {
                    platformCategoryId = parsedCatId;
                }
            }

            if (platformCategoryId == 0)
            {
                _logger.LogError("Trendyol PushProduct ABORTED: categoryId=0 for SKU={SKU}. " +
                    "Trendyol API requires a leaf-level integer categoryId. Map via PlatformMapping.ExternalCategoryId.", product.SKU);
                return false;
            }

            // dimensionalWeight (desi) — Trendyol zorunlu alan
            var dimensionalWeight = product.Desi ?? 1m;

            // cargoCompanyId — Trendyol zorunlu (varsayılan: Yurtiçi=17)
            const int defaultCargoCompanyId = 17;

            // Images — Trendyol requires at least 1, supports up to 8 image URLs
            var imageUrls = new List<object>();
            if (!string.IsNullOrEmpty(product.ImageUrl))
                imageUrls.Add(new { url = product.ImageUrl });

            // Additional images from PlatformSpecificData JSON: { "images": ["url1","url2"] }
            var trendyolPlatformMapping = product.PlatformMappings?
                .FirstOrDefault(m => m.PlatformType == PlatformType.Trendyol);
            var platformData = ParsePlatformSpecificData(trendyolPlatformMapping?.PlatformSpecificData);

            if (platformData.TryGetValue("images", out var extraImagesElement)
                && extraImagesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var img in extraImagesElement.EnumerateArray())
                {
                    var url = img.GetString();
                    if (!string.IsNullOrWhiteSpace(url))
                        imageUrls.Add(new { url });
                }
            }

            if (imageUrls.Count == 0)
            {
                _logger.LogError("Trendyol PushProduct ABORTED: no images for SKU={SKU}. " +
                    "Trendyol requires at least 1 product image.", product.SKU);
                return false;
            }

            // Attributes — Trendyol requires category-specific attributes
            // Format: [{ attributeId: 123, attributeValueId: 456 }]
            // Read from PlatformSpecificData JSON: { "attributes": [{"attributeId":1,"attributeValueId":2}] }
            var attributes = new List<object>();
            if (platformData.TryGetValue("attributes", out var attrsElement)
                && attrsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var attr in attrsElement.EnumerateArray())
                {
                    if (attr.TryGetProperty("attributeId", out var aid)
                        && attr.TryGetProperty("attributeValueId", out var avid))
                    {
                        attributes.Add(new { attributeId = aid.GetInt32(), attributeValueId = avid.GetInt32() });
                    }
                    else if (attr.TryGetProperty("attributeId", out var aid2)
                             && attr.TryGetProperty("customAttributeValue", out var cav))
                    {
                        attributes.Add(new { attributeId = aid2.GetInt32(), customAttributeValue = cav.GetString() });
                    }
                }
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
                        categoryId = platformCategoryId,
                        quantity = product.Stock,
                        stockCode = product.SKU,
                        dimensionalWeight = dimensionalWeight,
                        description = product.Description ?? "",
                        currencyType = "TRY",
                        listPrice = product.ListPrice ?? product.SalePrice,
                        salePrice = product.SalePrice,
                        vatRate = MapToTrendyolVatRate(product.TaxRate),
                        cargoCompanyId = defaultCargoCompanyId,
                        images = imageUrls,
                        attributes
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/product/sellers/{_supplierId}/v2/products", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol PushProduct failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            // Parse batchRequestId from Trendyol async response
            try
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var responseDoc = JsonDocument.Parse(responseBody);
                var batchId = responseDoc.RootElement.TryGetProperty("batchRequestId", out var bid)
                    ? bid.GetString() : null;

                _logger.LogInformation(
                    "Trendyol PushProduct accepted: SKU={SKU} BatchRequestId={BatchId}",
                    product.SKU, batchId);
            }
            catch (JsonException)
            {
                _logger.LogInformation("Trendyol PushProduct accepted: SKU={SKU} (response not JSON)", product.SKU);
            }

            return true;
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "Trendyol PushProduct: platform gecersiz yanit dondurdu — SKU={SKU}", product.SKU);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol PushProduct exception: {SKU}", product.SKU);
            return false;
        }
    }

    /// <summary>Pulls products with optional limit (for connection test panel).</summary>
    public Task<IReadOnlyList<Product>> PullProductsAsync(int limit, CancellationToken ct = default)
        => PullProductsInternalAsync(limit, null, null, ct);

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
        => PullProductsInternalAsync(null, null, null, ct);

    /// <summary>
    /// Barcode veya stockCode ile filtrelenmiş ürün çekme.
    /// Trendyol API: ?barcode={barcode}&amp;stockCode={stockCode}
    /// </summary>
    public Task<IReadOnlyList<Product>> PullProductsAsync(string? barcode, string? stockCode, CancellationToken ct = default)
        => PullProductsInternalAsync(null, barcode, stockCode, ct);

    /// <summary>
    /// Delta sync — Trendyol dateQueryType=LAST_MODIFIED_DATE ile sadece degisen urunleri ceker.
    /// D12-11: 9000 urun icin 200/sayfa = 45 API call = ~90 saniye.
    /// </summary>
    public async Task<ProductSyncResult> SyncProductsDeltaAsync(
        DateTime lastSyncTime, int pageSize = 200, CancellationToken ct = default)
    {
        EnsureConfigured();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var startDate = new DateTimeOffset(lastSyncTime).ToUnixTimeMilliseconds();
        var allProducts = new List<Product>();
        var page = 0;
        var apiCalls = 0;
        pageSize = Math.Clamp(pageSize, 1, 200); // Trendyol max 200

        _logger.LogInformation(
            "TrendyolAdapter.SyncProductsDeltaAsync: since={Since}, pageSize={PageSize}",
            lastSyncTime, pageSize);

        try
        {
            bool hasMore = true;
            while (hasMore)
            {
                ct.ThrowIfCancellationRequested();
                await ApplyRateLimitAsync(ct).ConfigureAwait(false);

                using var response = await _retryPipeline.ExecuteAsync(
                    async token =>
                    {
                        var queryUrl = $"/integration/product/sellers/{_supplierId}/products" +
                            $"?page={page}&size={pageSize}" +
                            $"&dateQueryType=LAST_MODIFIED_DATE&startDate={startDate}";
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri(queryUrl, UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);
                apiCalls++;

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError("DeltaSync page {Page} failed: {Status} — {Error}",
                        page, response.StatusCode, errorBody);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                var totalPages = doc.RootElement.TryGetProperty("totalPages", out var tp) ? tp.GetInt32() : 0;
                var totalElements = doc.RootElement.TryGetProperty("totalElements", out var te) ? te.GetInt32() : 0;

                if (doc.RootElement.TryGetProperty("content", out var contentArr))
                {
                    foreach (var item in contentArr.EnumerateArray())
                    {
                        allProducts.Add(MapJsonToProduct(item));
                    }
                }

                page++;
                hasMore = page < totalPages;
            }

            sw.Stop();
            _logger.LogInformation(
                "DeltaSync tamamlandı: {Count}/{Total} ürün, {Pages} sayfa, {Calls} API call, {Duration}s",
                allProducts.Count, allProducts.Count, page, apiCalls, sw.Elapsed.TotalSeconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            _logger.LogError(ex, "DeltaSync failed at page {Page}", page);
        }

        return new ProductSyncResult(
            allProducts.AsReadOnly(), allProducts.Count, page, apiCalls, sw.Elapsed, DateTime.UtcNow);
    }

    private async Task<IReadOnlyList<Product>> PullProductsInternalAsync(int? limit, string? barcodeFilter, string? stockCodeFilter, CancellationToken ct)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.PullProductsAsync called (limit={Limit})", limit?.ToString() ?? "all");

        // Pre-load brand/category platform mappings for FK resolution
        var brandMap = new Dictionary<string, Guid>();   // externalBrandId → Brand.Id
        var categoryMap = new Dictionary<string, Guid>(); // externalCategoryId → Category.Id
        if (_scopeFactory is not null)
        {
            try
            {
                using var lookupScope = _scopeFactory.CreateScope();
                var db = lookupScope.ServiceProvider.GetService<AppDbContext>();
                if (db is not null)
                {
                    var brandMappings = await db.BrandPlatformMappings
                        .Where(m => m.PlatformType == PlatformType.Trendyol && m.ExternalBrandId != null)
                        .Select(m => new { m.ExternalBrandId, m.BrandId })
                        .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
                    foreach (var bm in brandMappings)
                        brandMap.TryAdd(bm.ExternalBrandId!, bm.BrandId);

                    var catMappings = await db.Set<CategoryPlatformMapping>()
                        .Where(m => m.PlatformType == PlatformType.Trendyol && m.ExternalCategoryId != null)
                        .Select(m => new { m.ExternalCategoryId, m.CategoryId })
                        .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
                    foreach (var cm in catMappings)
                        categoryMap.TryAdd(cm.ExternalCategoryId!, cm.CategoryId);

                    _logger.LogDebug("PullProducts FK lookup loaded: {BrandCount} brands, {CatCount} categories",
                        brandMap.Count, categoryMap.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "PullProducts FK lookup failed — products will have empty BrandId/CategoryId");
            }
        }

        var products = new List<Product>();
        var page = 0;
        var pageSize = limit.HasValue ? Math.Min(limit.Value, 50) : 50;

        try
        {
            bool hasMore = true;
            while (hasMore)
            {
                await ApplyRateLimitAsync(ct).ConfigureAwait(false);

                using var response = await _retryPipeline.ExecuteAsync(
                    async token =>
                    {
                        var queryUrl = $"/integration/product/sellers/{_supplierId}/products?page={page}&size={pageSize}";
                        if (!string.IsNullOrWhiteSpace(barcodeFilter))
                            queryUrl += $"&barcode={Uri.EscapeDataString(barcodeFilter)}";
                        if (!string.IsNullOrWhiteSpace(stockCodeFilter))
                            queryUrl += $"&stockCode={Uri.EscapeDataString(stockCodeFilter)}";
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri(queryUrl, UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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
                        // Images: ilk resim → ImageUrl, tum resimler → PlatformSpecificData
                        string? imageUrl = null;
                        var allImageUrls = new List<string>();
                        if (item.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var img in images.EnumerateArray())
                            {
                                if (img.ValueKind == JsonValueKind.Object && img.TryGetProperty("url", out var imgUrl))
                                {
                                    var url = imgUrl.GetString();
                                    if (!string.IsNullOrWhiteSpace(url))
                                    {
                                        imageUrl ??= url;
                                        allImageUrls.Add(url);
                                    }
                                }
                            }
                        }

                        // SKU resolution: stockCode → productMainId → barcode (Trendyol API often returns stockCode=null)
                        var skuValue = item.TryGetProperty("stockCode", out var sc) && sc.ValueKind == JsonValueKind.String ? sc.GetString() : null;
                        if (string.IsNullOrEmpty(skuValue))
                            skuValue = item.TryGetProperty("productMainId", out var pmi2) && pmi2.ValueKind == JsonValueKind.String ? pmi2.GetString() : null;
                        if (string.IsNullOrEmpty(skuValue))
                            skuValue = item.TryGetProperty("barcode", out var bc2) ? bc2.GetString() : null;

                        // FK resolution — brandId → BrandPlatformMapping → Product.BrandId
                        var extBrandId = item.TryGetProperty("brandId", out var bi) ? bi.GetInt64().ToString() : null;
                        Guid? resolvedBrandId = null;
                        if (extBrandId is not null && brandMap.TryGetValue(extBrandId, out var mappedBrandId))
                            resolvedBrandId = mappedBrandId;

                        // FK resolution — categoryId → CategoryPlatformMapping → Product.CategoryId
                        var extCategoryId = item.TryGetProperty("pimCategoryId", out var ci) ? ci.GetInt32().ToString()
                            : item.TryGetProperty("categoryId", out var ci2) ? ci2.GetInt32().ToString() : null;
                        var resolvedCategoryId = Guid.Empty;
                        if (extCategoryId is not null && categoryMap.TryGetValue(extCategoryId, out var mappedCatId))
                            resolvedCategoryId = mappedCatId;

                        // Notes — ek resimler + platform metadata (PlatformMapping StoreId gerektirir, caller olusturur)
                        string? notes = null;
                        if (allImageUrls.Count > 1 || extBrandId is not null || extCategoryId is not null)
                        {
                            notes = JsonSerializer.Serialize(new
                            {
                                images = allImageUrls,
                                trendyolBrandId = extBrandId,
                                trendyolCategoryId = extCategoryId,
                                gender = item.TryGetProperty("gender", out var gen) ? gen.GetString() : null
                            }, _jsonOptions);
                        }

                        var product = new Product
                        {
                            Id = Guid.NewGuid(),
                            Name = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                            SKU = skuValue ?? "",
                            Barcode = item.TryGetProperty("barcode", out var b) ? b.GetString() : null,
                            SalePrice = item.TryGetProperty("salePrice", out var sp) ? sp.GetDecimal() : 0,
                            ListPrice = item.TryGetProperty("listPrice", out var lp) ? lp.GetDecimal() : null,
                            Description = item.TryGetProperty("description", out var d) ? d.GetString() : null,
                            TaxRate = item.TryGetProperty("vatRate", out var vr) ? vr.GetDecimal() / 100m : 0.18m,
                            ImageUrl = imageUrl,
                            Code = item.TryGetProperty("productMainId", out var pmi) ? pmi.GetString() : null,
                            Color = item.TryGetProperty("color", out var clr2) ? clr2.GetString() : null,
                            CurrencyCode = item.TryGetProperty("currencyType", out var ccy) ? ccy.GetString() ?? "TRY" : "TRY",
                            BrandId = resolvedBrandId,
                            CategoryId = resolvedCategoryId,
                            Notes = notes
                        };
                        product.SyncStock(item.TryGetProperty("quantity", out var q) ? q.GetInt32() : 0, "trendyol-sync");
                        products.Add(product);
                    }
                }

                page++;
                hasMore = page < totalPages;

                // Limit support for connection test panel
                if (limit.HasValue && products.Count >= limit.Value)
                {
                    products = products.Take(limit.Value).ToList();
                    break;
                }
            }

            _logger.LogInformation("Trendyol PullProducts: {Count} products retrieved", products.Count);
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "Trendyol PullProducts: platform gecersiz yanit dondurdu — page={Page}", page);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            var barcode = await ResolveBarcodeAsync(productId, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(barcode))
            {
                _logger.LogError("Trendyol StockUpdate ABORTED: no barcode mapping for ProductId={ProductId}. " +
                    "ProductPlatformMapping with PlatformType.Trendyol required.", productId);
                return false;
            }

            var payload = new
            {
                items = new[]
                {
                    new { barcode, quantity = newStock }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/inventory/sellers/{_supplierId}/products/price-and-inventory", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol StockUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            LogBatchRequestId(response, "StockUpdate", barcode, ct);
            return true;
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "Trendyol StockUpdate: platform gecersiz yanit dondurdu — ProductId={ProductId}", productId);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            var barcode = await ResolveBarcodeAsync(productId, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(barcode))
            {
                _logger.LogError("Trendyol PriceUpdate ABORTED: no barcode mapping for ProductId={ProductId}. " +
                    "ProductPlatformMapping with PlatformType.Trendyol required.", productId);
                return false;
            }

            var payload = new
            {
                items = new[]
                {
                    new { barcode, listPrice = newPrice, salePrice = newPrice }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/inventory/sellers/{_supplierId}/products/price-and-inventory", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol PriceUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            LogBatchRequestId(response, "PriceUpdate", barcode, ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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

                using var response = await _retryPipeline.ExecuteAsync(
                    async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri(url, UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogError(
                        "Trendyol PullOrders page {Page} failed: {Status} — {FetchedCount} orders fetched so far (PARTIAL DATA). Error: {Error}",
                        page, response.StatusCode, orders.Count, errorBody);
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
                            TotalAmount = item.TryGetProperty("totalPrice", out var tp2) && tp2.ValueKind == JsonValueKind.Number ? tp2.GetDecimal()
                                : item.TryGetProperty("packageTotalPrice", out var ptp) && ptp.ValueKind == JsonValueKind.Number ? ptp.GetDecimal() : 0,
                            GrossAmount = item.TryGetProperty("grossAmount", out var ga) && ga.ValueKind == JsonValueKind.Number ? ga.GetDecimal()
                                : item.TryGetProperty("packageGrossAmount", out var pga) && pga.ValueKind == JsonValueKind.Number ? pga.GetDecimal() : null,
                            TotalDiscount = item.TryGetProperty("totalDiscount", out var td) && td.ValueKind == JsonValueKind.Number ? td.GetDecimal()
                                : item.TryGetProperty("packageTotalDiscount", out var ptd) && ptd.ValueKind == JsonValueKind.Number ? ptd.GetDecimal() : null,
                            OrderDate = item.TryGetProperty("orderDate", out var od) ? DateTimeOffset.FromUnixTimeMilliseconds(od.GetInt64()).UtcDateTime : DateTime.UtcNow
                        };

                        // Siparis satir detaylari
                        if (item.TryGetProperty("lines", out var lines))
                        {
                            foreach (var line in lines.EnumerateArray())
                            {
                                order.Lines.Add(new ExternalOrderLineDto
                                {
                                    PlatformLineId = line.TryGetProperty("lineId", out var lid) ? lid.GetInt64().ToString()
                                        : line.TryGetProperty("id", out var lid2) ? lid2.GetInt64().ToString() : null,
                                    SKU = ResolveLineSku(line),
                                    Barcode = line.TryGetProperty("barcode", out var bc) ? bc.GetString() : null,
                                    ProductName = line.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "",
                                    Quantity = line.TryGetProperty("quantity", out var qty) ? qty.GetInt32() : 1,
                                    UnitPrice = line.TryGetProperty("price", out var up) && up.ValueKind == JsonValueKind.Number ? up.GetDecimal()
                                        : line.TryGetProperty("lineUnitPrice", out var lup) && lup.ValueKind == JsonValueKind.Number ? lup.GetDecimal() : 0,
                                    DiscountAmount = line.TryGetProperty("discount", out var disc) && disc.ValueKind == JsonValueKind.Number ? disc.GetDecimal()
                                        : line.TryGetProperty("lineTotalDiscount", out var ltd) && ltd.ValueKind == JsonValueKind.Number ? ltd.GetDecimal() : null,
                                    CommissionAmount = line.TryGetProperty("commission", out var comm) && comm.ValueKind == JsonValueKind.Number ? comm.GetDecimal() : null,
                                    TaxRate = line.TryGetProperty("vatRate", out var lvr) ? lvr.GetDecimal() / 100m : 0m,
                                    LineTotal = line.TryGetProperty("amount", out var amt) && amt.ValueKind == JsonValueKind.Number ? amt.GetDecimal()
                                        : line.TryGetProperty("lineGrossAmount", out var lga) && lga.ValueKind == JsonValueKind.Number ? lga.GetDecimal() : 0
                                });
                            }
                        }

                        // Kargo bilgisi
                        if (item.TryGetProperty("shipmentPackageId", out var spId))
                            order.ShipmentPackageId = spId.GetInt64().ToString();
                        if (item.TryGetProperty("cargoProviderName", out var cpn))
                            order.CargoProviderName = cpn.GetString();
                        if (item.TryGetProperty("cargoTrackingNumber", out var ctn))
                            order.CargoTrackingNumber = ctn.ValueKind == JsonValueKind.String
                                ? ctn.GetString()
                                : ctn.ValueKind == JsonValueKind.Number ? ctn.GetInt64().ToString() : null;

                        // Fatura linki — Trendyol tarafından oluşturulan fatura PDF'i
                        if (item.TryGetProperty("invoiceLink", out var invLink))
                            order.InvoiceLink = invLink.GetString();

                        // Müşteri email + adres
                        if (item.TryGetProperty("customerEmail", out var email))
                            order.CustomerEmail = email.GetString();
                        if (item.TryGetProperty("shipmentAddress", out var addr))
                        {
                            order.CustomerAddress = addr.TryGetProperty("fullAddress", out var fa) ? fa.GetString() : null;
                            order.CustomerCity = addr.TryGetProperty("city", out var city) ? city.GetString() : null;
                            if (string.IsNullOrEmpty(order.CustomerPhone))
                                order.CustomerPhone = addr.TryGetProperty("phone", out var ph) ? ph.GetString() : null;
                        }

                        // Fatura adresi — e-fatura (UBL-TR) kesimi icin kritik
                        if (item.TryGetProperty("invoiceAddress", out var invAddr))
                        {
                            order.InvoiceAddress = invAddr.TryGetProperty("fullAddress", out var ifa) ? ifa.GetString() : null;
                            order.InvoiceCity = invAddr.TryGetProperty("city", out var icity) ? icity.GetString() : null;
                            order.InvoiceDistrict = invAddr.TryGetProperty("district", out var idistr) ? idistr.GetString() : null;
                            order.InvoiceFullName = invAddr.TryGetProperty("fullName", out var ifn) ? ifn.GetString() : null;
                            // TC/Vergi No — Trendyol invoiceAddress icinde doner
                            if (string.IsNullOrEmpty(order.CustomerTaxNumber) && invAddr.TryGetProperty("taxNumber", out var taxNo))
                                order.CustomerTaxNumber = taxNo.GetString();
                        }

                        // Son güncelleme tarihi
                        if (item.TryGetProperty("lastModifiedDate", out var lmd) && lmd.TryGetInt64(out var lmdMs))
                            order.LastModifiedDate = DateTimeOffset.FromUnixTimeMilliseconds(lmdMs).UtcDateTime;

                        // Para birimi
                        if (item.TryGetProperty("currencyCode", out var cc))
                            order.Currency = cc.GetString() ?? "TRY";

                        orders.Add(order);
                    }
                }

                page++;
                hasMore = page < totalPages;
            }

            _logger.LogInformation("Trendyol PullOrders: {Count} orders retrieved", orders.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Put,
                        new Uri($"/integration/order/sellers/{_supplierId}/orders/shipment-packages/{packageId}", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol UpdateOrderStatus failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol UpdateOrderStatus exception: {PackageId}", packageId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // IShipmentCapableAdapter — Kargo Bildirimi

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
            CargoProvider.Sendeo => "Sendeo",
            CargoProvider.UPS => "UPS",
            _ => provider.ToString()
        };

        _logger.LogInformation(
            "TrendyolAdapter.SendShipmentAsync: Package={PackageId} Tracking={Tracking} Cargo={Cargo}",
            platformOrderId, trackingNumber, cargoCompany);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            if (!long.TryParse(platformOrderId, out var packageIdLong))
            {
                _logger.LogError("SendShipment: Invalid shipmentPackageId '{Id}'", platformOrderId);
                return false;
            }

            var payload = new
            {
                shipmentPackageId = packageIdLong,
                trackingNumber,
                cargoCompany
            };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Put,
                        new Uri($"/integration/order/sellers/{_supplierId}/orders/shipment-packages/{packageIdLong}", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol SendShipment failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol SendShipment OK: Package={PackageId}", platformOrderId);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol SendShipment exception: Package={PackageId}", platformOrderId);
            return false;
        }
    }

    /// <summary>
    /// Trendyol tedarik edilemeyen siparis bildirimi.
    /// PUT /sapigw/suppliers/{supplierId}/shipment-packages/{packageId} body: {"status":"UnSupplied"}
    /// </summary>
    public async Task<bool> MarkPackageUnsuppliedAsync(string packageId, CancellationToken ct = default)
    {
        EnsureConfigured();

        if (!long.TryParse(packageId, out var packageIdLong))
        {
            _logger.LogError("[Trendyol] MarkUnsupplied: Invalid packageId '{Id}'", packageId);
            return false;
        }

        _logger.LogInformation("[Trendyol] MarkPackageUnsupplied: PackageId={PackageId}", packageId);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = JsonSerializer.Serialize(new { status = "UnSupplied" }, _jsonOptions);

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Put,
                        new Uri($"/integration/order/sellers/{_supplierId}/shipment-packages/{packageIdLong}", UriKind.Relative),
                        new StringContent(payload, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("[Trendyol] MarkUnsupplied failed: {Status} — {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("[Trendyol] MarkUnsupplied OK: PackageId={PackageId}", packageId);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[Trendyol] MarkUnsupplied exception: PackageId={PackageId}", packageId);
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/order/sellers/{_supplierId}/orders/invoiceLinks", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol SendInvoiceLink failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                            new Uri($"/integration/order/sellers/{_supplierId}/orders/invoice-file", UriKind.Relative), formContent);
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol SendInvoiceFile failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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

                using var response = await _retryPipeline.ExecuteAsync(
                    async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri(url, UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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
                        // claimId is UUID string (not int64)
                        var claimIdStr = item.TryGetProperty("claimId", out var cidProp)
                            ? cidProp.GetString() ?? ""
                            : item.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";

                        var firstName = item.TryGetProperty("customerFirstName", out var fn) ? fn.GetString() ?? "" : "";
                        var lastName = item.TryGetProperty("customerLastName", out var ln) ? ln.GetString() ?? "" : "";

                        var claim = new ExternalClaimDto
                        {
                            PlatformCode = PlatformCode,
                            PlatformClaimId = claimIdStr,
                            OrderNumber = item.TryGetProperty("orderNumber", out var on) ? on.GetString() ?? "" : "",
                            Status = item.TryGetProperty("claimStatus", out var st) ? st.GetString() ?? ""
                                : item.TryGetProperty("status", out var st2) ? st2.GetString() ?? "" : "",
                            Reason = item.TryGetProperty("claimIssueReasonText", out var r) ? r.GetString() ?? ""
                                : item.TryGetProperty("reason", out var r2) ? r2.GetString() ?? "" : "",
                            ReasonDetail = item.TryGetProperty("claimIssueReasonText", out var rd) ? rd.GetString() : null,
                            CustomerName = $"{firstName} {lastName}".Trim(),
                            ClaimDate = item.TryGetProperty("claimDate", out var cd) && cd.TryGetInt64(out var cdMs)
                                ? DateTimeOffset.FromUnixTimeMilliseconds(cdMs).UtcDateTime
                                : DateTime.UtcNow
                        };

                        // Trendyol claim items: items[].orderLine.productName
                        if (item.TryGetProperty("items", out var claimItems))
                        {
                            foreach (var ci in claimItems.EnumerateArray())
                            {
                                var orderLine = ci.TryGetProperty("orderLine", out var ol) ? ol : ci;
                                claim.Lines.Add(new ExternalClaimLineDto
                                {
                                    Barcode = orderLine.TryGetProperty("barcode", out var bc) ? bc.GetString() : null,
                                    ProductName = orderLine.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "",
                                    Quantity = orderLine.TryGetProperty("quantity", out var qty) ? qty.GetInt32() : 1,
                                    UnitPrice = orderLine.TryGetProperty("price", out var up) ? up.GetDecimal() : 0
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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/order/sellers/{_supplierId}/claims/{claimId}/approve", UriKind.Relative),
                        new StringContent("{}", Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol ApproveClaim failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/order/sellers/{_supplierId}/claims/{claimId}/issue", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol RejectClaim failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri($"/integration/finance/sellers/{_supplierId}/settlement?startDate={startEpoch}&endDate={endEpoch}", UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol GetSettlement exception");
            return null;
        }
    }

    /// <summary>
    /// Trendyol komisyon kesinti faturalari.
    /// GET /integration/finance/sellers/{supplierId}/deduction-invoices?startDate={epoch}
    /// </summary>
    public async Task<IReadOnlyList<CargoInvoiceDto>> GetDeductionInvoicesAsync(DateTime startDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("[Trendyol] GetDeductionInvoicesAsync since={StartDate}", startDate);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var startEpoch = new DateTimeOffset(startDate).ToUnixTimeMilliseconds();

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                        new Uri($"/integration/finance/sellers/{_supplierId}/deduction-invoices?startDate={startEpoch}", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("[Trendyol] GetDeductionInvoices failed: {Status} — {Error}", response.StatusCode, error);
                return Array.Empty<CargoInvoiceDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var invoices = new List<CargoInvoiceDto>();
            var items = doc.RootElement.TryGetProperty("content", out var arr) ? arr : doc.RootElement;

            if (items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    invoices.Add(new CargoInvoiceDto
                    {
                        InvoiceNumber = item.TryGetProperty("invoiceNumber", out var inv) ? inv.GetString() ?? "" : "",
                        InvoiceDate = item.TryGetProperty("invoiceDate", out var dt) && dt.ValueKind == JsonValueKind.Number
                            ? DateTimeOffset.FromUnixTimeMilliseconds(dt.GetInt64()).DateTime
                            : DateTime.UtcNow,
                        Amount = item.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0m,
                        TotalAmount = item.TryGetProperty("totalAmount", out var tot) ? tot.GetDecimal() : 0m,
                        TaxAmount = item.TryGetProperty("taxAmount", out var tax) ? tax.GetDecimal() : 0m,
                        CargoCompany = "Trendyol Deduction"
                    });
                }
            }

            _logger.LogInformation("[Trendyol] GetDeductionInvoices: {Count} fatura", invoices.Count);
            return invoices;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[Trendyol] GetDeductionInvoices exception");
            return Array.Empty<CargoInvoiceDto>();
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri($"/integration/finance/sellers/{_supplierId}/cargo-invoices?startDate={startEpoch}", UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri("/integration/product/product-categories", UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

        // Cache check — CategoryPlatformMapping.CachedAttributesJson (24h TTL)
        if (_scopeFactory is not null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var catMappingRepo = scope.ServiceProvider.GetService<ICategoryPlatformMappingRepository>();
                var tenantProvider = scope.ServiceProvider.GetService<ITenantProvider>();
                if (catMappingRepo is not null && tenantProvider is not null)
                {
                    var tenantId = tenantProvider.GetCurrentTenantId();
                    var mapping = await catMappingRepo.GetByExternalCategoryIdAsync(
                        tenantId, categoryId.ToString(), PlatformType.Trendyol, ct).ConfigureAwait(false);

                    if (mapping?.CachedAttributesJson is not null
                        && mapping.AttributesCachedAt.HasValue
                        && mapping.AttributesCachedAt.Value > DateTime.UtcNow.AddHours(-24))
                    {
                        var cached = JsonSerializer.Deserialize<List<CategoryAttributeDto>>(
                            mapping.CachedAttributesJson, _jsonOptions);
                        if (cached is { Count: > 0 })
                        {
                            _logger.LogDebug(
                                "Trendyol GetCategoryAttributes cache HIT: {Count} attributes for category {CategoryId}",
                                cached.Count, categoryId);
                            return cached.AsReadOnly();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "CategoryAttribute cache lookup failed — falling through to API");
            }
        }

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri($"/integration/product/product-categories/{categoryId}/attributes", UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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

            // Cache write — API sonucunu CategoryPlatformMapping.CachedAttributesJson'a yaz
            if (attributes.Count > 0 && _scopeFactory is not null)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var catMappingRepo = scope.ServiceProvider.GetService<ICategoryPlatformMappingRepository>();
                    var tenantProvider = scope.ServiceProvider.GetService<ITenantProvider>();
                    var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();
                    if (catMappingRepo is not null && tenantProvider is not null && unitOfWork is not null)
                    {
                        var tenantId = tenantProvider.GetCurrentTenantId();
                        var mapping = await catMappingRepo.GetByExternalCategoryIdAsync(
                            tenantId, categoryId.ToString(), PlatformType.Trendyol, ct).ConfigureAwait(false);
                        if (mapping is not null)
                        {
                            mapping.CachedAttributesJson = JsonSerializer.Serialize(attributes, _jsonOptions);
                            mapping.AttributesCachedAt = DateTime.UtcNow;
                            await catMappingRepo.UpdateAsync(mapping, ct).ConfigureAwait(false);
                            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                            _logger.LogDebug("Trendyol CategoryAttributes cache WRITE: {Count} attrs for category {CategoryId}", attributes.Count, categoryId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "CategoryAttribute cache write failed — non-critical, API data still returned");
                }
            }

            return attributes.AsReadOnly();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri($"/integration/product/sellers/{_supplierId}/products/batch-requests/{batchRequestId}", UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri($"/integration/product/brands?name={Uri.EscapeDataString(namePrefix)}", UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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
        catch (Exception ex) when (ex is not OperationCanceledException)
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
            using var statusRequest = CreateAuthenticatedRequest(HttpMethod.Get,
                new Uri("/integration/product/api-status", UriKind.Relative));
            using var response = await _httpClient.SendAsync(statusRequest, ct).ConfigureAwait(false);

            sw.Stop();
            result.LatencyMs = (int)sw.ElapsedMilliseconds;
            result.IsHealthy = response.IsSuccessStatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {body[..Math.Min(body.Length, 200)]}";
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/order/sellers/{_supplierId}/webhooks", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol RegisterWebhook exception");
            return false;
        }
    }

    public async Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.UnregisterWebhookAsync");

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            // Trendyol webhook silme: GET ile mevcut webhook'ları listele, sonra DELETE
            using var listResponse = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                        new Uri($"/integration/order/sellers/{_supplierId}/webhooks", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!listResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Trendyol ListWebhooks failed: {Status}", listResponse.StatusCode);
                return false;
            }

            var content = await listResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            // Trendyol webhook list response: array of { id, url, ... }
            var webhooks = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray()
                : (doc.RootElement.TryGetProperty("content", out var arr) ? arr.EnumerateArray() : default);

            foreach (var wh in webhooks)
            {
                var whId = wh.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
                if (whId <= 0) continue;

                await ApplyRateLimitAsync(ct).ConfigureAwait(false);

                using var delResponse = await _retryPipeline.ExecuteAsync(
                    async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Delete,
                            new Uri($"/integration/order/sellers/{_supplierId}/webhooks/{whId}", UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

                _logger.LogInformation("Trendyol webhook {Id} deleted: {Status}", whId, delResponse.StatusCode);
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol UnregisterWebhook exception");
            return false;
        }
    }

    public async Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var eventType = doc.RootElement.TryGetProperty("eventType", out var et) ? et.GetString() : "unknown";
            var orderId = doc.RootElement.TryGetProperty("orderNumber", out var on) ? on.GetString() : null;

            _logger.LogInformation(
                "TrendyolAdapter webhook received: EventType={EventType} OrderId={OrderId} PayloadLength={Length}",
                eventType, orderId, payload.Length);

            // Delegate to WebhookDispatchHelper — handles replay protection + MediatR dispatch
            await WebhookDispatchHelper.DispatchAsync(
                _scopeFactory, PlatformCode, eventType, orderId, payload, _logger, ct)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[TrendyolAdapter] Malformed webhook payload (length={Length})", payload?.Length ?? 0);
        }
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/product/sellers/{_supplierId}/v2/products/archive", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/product/sellers/{_supplierId}/v2/products/unlock", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri($"/integration/product/sellers/{_supplierId}/questions?page={page}&size={size}", UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/product/sellers/{_supplierId}/questions/{questionId}/answers", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri(url, UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Put,
                        new Uri($"/integration/order/sellers/{_supplierId}/claims/{claimId}/approve", UriKind.Relative),
                        new StringContent("{}", Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Put,
                        new Uri($"/integration/order/sellers/{_supplierId}/claims/{claimId}/reject", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/order/sellers/{_supplierId}/invoices", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri($"/integration/finance/sellers/{_supplierId}/settlements?startDate={startEpoch}&endDate={endEpoch}", UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/order/sellers/{_supplierId}/packages/{packageId}/split", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Put,
                        new Uri($"/integration/order/sellers/{_supplierId}/packages/{packageId}/box-info", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                    {
                        using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                            new Uri($"/integration/order/sellers/{_supplierId}/claims/compensation", UriKind.Relative));
                        return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    }, ct).ConfigureAwait(false);

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
        catch (Exception ex) when (ex is not OperationCanceledException)
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

    /// <summary>Creates an HttpRequestMessage with per-request Authorization and User-Agent headers (thread-safe).</summary>
    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, Uri uri)
    {
        var request = new HttpRequestMessage(method, uri);
        if (_authHeader is not null)
            request.Headers.Authorization = _authHeader;
        request.Headers.TryAddWithoutValidation("User-Agent", "MesTech-Trendyol-Client/3.0");
        return request;
    }

    /// <summary>Creates an HttpRequestMessage with per-request Authorization, User-Agent, and body content (thread-safe).</summary>
    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, Uri uri, HttpContent content)
    {
        var request = CreateAuthenticatedRequest(method, uri);
        request.Content = content;
        return request;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "TrendyolAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
    }

    /// <summary>
    /// PlatformSpecificData JSON string'ini Dictionary'ye parse eder.
    /// Boş/null ise boş dictionary döner.
    /// </summary>
    private static Dictionary<string, JsonElement> ParsePlatformSpecificData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, JsonElement>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var result = new Dictionary<string, JsonElement>();
            foreach (var prop in doc.RootElement.EnumerateObject())
                result[prop.Name] = prop.Value.Clone();
            return result;
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>();
        }
    }

    private async Task ApplyRateLimitAsync(CancellationToken ct = default)
    {
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Interlocked.Increment(ref _totalRequests);
            await Task.Delay(10, ct).ConfigureAwait(false); // min 10ms between requests
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    // ═══════════════════════════════════════════
    // Product Update / Delete / Package Cancel
    // ═══════════════════════════════════════════

    /// <summary>Updates an existing product on Trendyol (PUT /v2/products).</summary>
    public async Task<bool> UpdateProductAsync(Product product, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        EnsureConfigured();

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                items = new[]
                {
                    new
                    {
                        barcode = product.Barcode ?? product.SKU,
                        title = product.Name,
                        productMainId = product.SKU,
                        stockCode = product.SKU,
                        dimensionalWeight = product.Desi ?? 1m,
                        description = product.Description ?? "",
                        currencyType = product.CurrencyCode,
                        listPrice = product.ListPrice ?? product.SalePrice,
                        salePrice = product.SalePrice,
                        quantity = product.Stock,
                        vatRate = (int)(product.TaxRate * 100),
                        cargoCompanyId = 17
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Put,
                        new Uri($"/integration/product/sellers/{_supplierId}/v2/products", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol UpdateProduct failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol UpdateProduct success: {SKU}", product.SKU);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol UpdateProduct exception: {SKU}", product.SKU);
            return false;
        }
    }

    /// <summary>Deletes (archives) a product on Trendyol by barcode.</summary>
    public async Task<bool> DeleteProductAsync(string barcode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);
        EnsureConfigured();

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new { items = new[] { new { barcode } } };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Delete,
                        new Uri($"/integration/product/sellers/{_supplierId}/v2/products", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol DeleteProduct failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol DeleteProduct success: {Barcode}", barcode);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol DeleteProduct exception: {Barcode}", barcode);
            return false;
        }
    }

    /// <summary>Cancels a shipment package on Trendyol (PUT /shipmentpackages/{packageId}/cancel).</summary>
    public async Task<bool> CancelPackageAsync(long packageId, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Put,
                        new Uri($"/integration/order/sellers/{_supplierId}/shipmentpackages/{packageId}/cancel", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol CancelPackage failed: {PackageId} {Status} - {Error}", packageId, response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol CancelPackage success: {PackageId}", packageId);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol CancelPackage exception: {PackageId}", packageId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // Shipment Providers / Addresses / Tracking
    // ═══════════════════════════════════════════

    /// <summary>Gets available shipment (cargo) providers from Trendyol.</summary>
    public async Task<JsonDocument?> GetShipmentProvidersAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);
            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                        new Uri("/integration/shipping-companies", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Trendyol GetShipmentProviders failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonDocument.Parse(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol GetShipmentProviders exception");
            return null;
        }
    }

    /// <summary>Gets seller addresses from Trendyol (return/shipment addresses).</summary>
    public async Task<JsonDocument?> GetSellerAddressesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);
            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                        new Uri($"/integration/sellers/{_supplierId}/addresses", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Trendyol GetSellerAddresses failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonDocument.Parse(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol GetSellerAddresses exception");
            return null;
        }
    }

    /// <summary>Gets shipment tracking details for a specific package.</summary>
    public async Task<JsonDocument?> GetTrackingDetailsAsync(long shipmentPackageId, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);
            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                        new Uri($"/integration/order/sellers/{_supplierId}/shipmentpackages/{shipmentPackageId}/tracking", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Trendyol GetTrackingDetails failed: {PackageId} {Status}", shipmentPackageId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonDocument.Parse(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol GetTrackingDetails exception: {PackageId}", shipmentPackageId);
            return null;
        }
    }

    // ═══════════════════════════════════════════
    // Webhook List / CurrentAccount / Claim Audit
    // ═══════════════════════════════════════════

    /// <summary>Lists currently registered webhooks for this seller.</summary>
    public async Task<JsonDocument?> ListWebhooksAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);
            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                        new Uri($"/integration/sellers/{_supplierId}/webhooks", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Trendyol ListWebhooks failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonDocument.Parse(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol ListWebhooks exception");
            return null;
        }
    }

    /// <summary>Gets current seller account information from Trendyol.</summary>
    public async Task<JsonDocument?> GetCurrentAccountAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);
            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                        new Uri($"/integration/sellers/{_supplierId}", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Trendyol GetCurrentAccount failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonDocument.Parse(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol GetCurrentAccount exception");
            return null;
        }
    }

    /// <summary>Gets claim audit/history details for a specific claim.</summary>
    public async Task<JsonDocument?> GetClaimAuditAsync(long claimId, CancellationToken ct = default)
    {
        EnsureConfigured();
        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);
            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                        new Uri($"/integration/order/sellers/{_supplierId}/claims/{claimId}", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Trendyol GetClaimAudit failed: {ClaimId} {Status}", claimId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonDocument.Parse(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol GetClaimAudit exception: {ClaimId}", claimId);
            return null;
        }
    }

    // ═══════════════════════════════════════════
    // Product Reviews (Ürün Değerlendirme)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Gets product reviews for the seller with optional filters.
    /// GET /sapigw/sellers/{supplierId}/products/reviews?page={page}&amp;size={size}&amp;...
    /// </summary>
    /// <param name="page">Sayfa numarasi (0-based).</param>
    /// <param name="size">Sayfa boyutu.</param>
    /// <param name="productId">Belirli bir urun icin filtrele (null = tum urunler).</param>
    /// <param name="minRating">Minimum yildiz filtresi (1-5, null = filtre yok).</param>
    /// <param name="unrepliedOnly">Sadece cevapsiz review'lari getir.</param>
    /// <param name="ct">Cancellation token.</param>
    // Explicit interface implementation — 2-param signature required by IReviewCapableAdapter
    async Task<IReadOnlyList<TrendyolProductReviewDto>> IReviewCapableAdapter.GetProductReviewsAsync(
        int page, int size, CancellationToken ct)
        => await GetProductReviewsAsync(page, size, ct: ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<TrendyolProductReviewDto>> GetProductReviewsAsync(
        int page = 0, int size = 20, long? productId = null,
        int? minRating = null, bool unrepliedOnly = false, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetProductReviewsAsync page={Page} size={Size} productId={ProductId} minRating={MinRating} unrepliedOnly={UnrepliedOnly}",
            page, size, productId, minRating, unrepliedOnly);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var queryParams = $"page={page}&size={size}";
            if (productId.HasValue) queryParams += $"&productId={productId.Value}";
            if (minRating.HasValue) queryParams += $"&minRate={minRating.Value}";
            if (unrepliedOnly) queryParams += "&isReplied=false";

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                        new Uri($"/integration/product/sellers/{_supplierId}/reviews?{queryParams}", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol GetProductReviews failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<TrendyolProductReviewDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var reviews = new List<TrendyolProductReviewDto>();
            if (doc.RootElement.TryGetProperty("content", out var contentArr))
            {
                foreach (var item in contentArr.EnumerateArray())
                {
                    reviews.Add(new TrendyolProductReviewDto(
                        Id: item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                        ProductId: item.TryGetProperty("productId", out var pid) ? pid.GetInt64() : 0,
                        Comment: item.TryGetProperty("comment", out var comment) ? comment.GetString() ?? "" : "",
                        Rate: item.TryGetProperty("rate", out var rate) ? rate.GetInt32() : 0,
                        UserFullName: item.TryGetProperty("userFullName", out var name) ? name.GetString() ?? "" : "",
                        CreatedAt: item.TryGetProperty("createdDate", out var cd) && cd.TryGetInt64(out var ts)
                            ? DateTimeOffset.FromUnixTimeMilliseconds(ts).UtcDateTime
                            : DateTime.MinValue,
                        IsReplied: item.TryGetProperty("isReplied", out var replied) && replied.GetBoolean()));
                }
            }

            _logger.LogInformation("Trendyol GetProductReviews: {Count} reviews fetched", reviews.Count);
            return reviews;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol GetProductReviews exception");
            return Array.Empty<TrendyolProductReviewDto>();
        }
    }

    /// <summary>
    /// Replies to a product review.
    /// POST /sapigw/sellers/{supplierId}/products/reviews/{reviewId}/replies
    /// </summary>
    public async Task<bool> ReplyToReviewAsync(long reviewId, string replyText, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replyText);
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.ReplyToReviewAsync: ReviewId={ReviewId}", reviewId);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new { text = replyText };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Post,
                        new Uri($"/integration/product/sellers/{_supplierId}/reviews/{reviewId}/replies", UriKind.Relative),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol ReplyToReview failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol ReplyToReview success: ReviewId={ReviewId}", reviewId);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol ReplyToReview exception: ReviewId={ReviewId}", reviewId);
            return false;
        }
    }

    // ═══════════════════════════════════════════
    // Trendyol Ads (Reklam API)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Gets active ad campaigns for the seller.
    /// GET /sapigw/sellers/{supplierId}/ads/campaigns
    /// </summary>
    public async Task<IReadOnlyList<TrendyolAdCampaignDto>> GetAdCampaignsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetAdCampaignsAsync");

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                        new Uri($"/integration/product/sellers/{_supplierId}/ads/campaigns", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol GetAdCampaigns failed: {Status} - {Error}", response.StatusCode, error);
                return Array.Empty<TrendyolAdCampaignDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var campaigns = new List<TrendyolAdCampaignDto>();
            if (doc.RootElement.TryGetProperty("content", out var contentArr))
            {
                foreach (var item in contentArr.EnumerateArray())
                {
                    campaigns.Add(new TrendyolAdCampaignDto(
                        CampaignId: item.TryGetProperty("campaignId", out var cid) ? cid.GetInt64() : 0,
                        Name: item.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                        Status: item.TryGetProperty("status", out var status) ? status.GetString() ?? "" : "",
                        DailyBudget: item.TryGetProperty("dailyBudget", out var db) ? db.GetDecimal() : 0,
                        TotalSpent: item.TryGetProperty("totalSpent", out var ts) ? ts.GetDecimal() : 0,
                        StartDate: item.TryGetProperty("startDate", out var sd) && sd.TryGetInt64(out var sdMs)
                            ? DateTimeOffset.FromUnixTimeMilliseconds(sdMs).UtcDateTime
                            : DateTime.MinValue,
                        EndDate: item.TryGetProperty("endDate", out var ed) && ed.TryGetInt64(out var edMs)
                            ? DateTimeOffset.FromUnixTimeMilliseconds(edMs).UtcDateTime
                            : null));
                }
            }

            _logger.LogInformation("Trendyol GetAdCampaigns: {Count} campaigns fetched", campaigns.Count);
            return campaigns;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol GetAdCampaigns exception");
            return Array.Empty<TrendyolAdCampaignDto>();
        }
    }

    /// <summary>
    /// Gets ad performance metrics for a campaign within a date range.
    /// GET /sapigw/sellers/{supplierId}/ads/campaigns/{campaignId}/performance?startDate={}&amp;endDate={}
    /// </summary>
    public async Task<IReadOnlyList<TrendyolAdPerformanceDto>> GetAdPerformanceAsync(long campaignId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.GetAdPerformanceAsync: CampaignId={CampaignId}", campaignId);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var startStr = startDate.ToString("yyyy-MM-dd");
            var endStr = endDate.ToString("yyyy-MM-dd");

            using var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var req = CreateAuthenticatedRequest(HttpMethod.Get,
                        new Uri($"/integration/product/sellers/{_supplierId}/ads/campaigns/{campaignId}/performance?startDate={startStr}&endDate={endStr}", UriKind.Relative));
                    return await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol GetAdPerformance failed: {CampaignId} {Status} - {Error}", campaignId, response.StatusCode, error);
                return Array.Empty<TrendyolAdPerformanceDto>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var metrics = new List<TrendyolAdPerformanceDto>();
            if (doc.RootElement.TryGetProperty("content", out var contentArr))
            {
                foreach (var item in contentArr.EnumerateArray())
                {
                    metrics.Add(new TrendyolAdPerformanceDto(
                        CampaignId: campaignId,
                        Impressions: item.TryGetProperty("impressions", out var imp) ? imp.GetInt64() : 0,
                        Clicks: item.TryGetProperty("clicks", out var clicks) ? clicks.GetInt64() : 0,
                        Ctr: item.TryGetProperty("ctr", out var ctr) ? ctr.GetDecimal() : 0,
                        Spend: item.TryGetProperty("spend", out var spend) ? spend.GetDecimal() : 0,
                        Revenue: item.TryGetProperty("revenue", out var rev) ? rev.GetDecimal() : 0,
                        Acos: item.TryGetProperty("acos", out var acos) ? acos.GetDecimal() : 0,
                        Date: item.TryGetProperty("date", out var d) && d.TryGetInt64(out var dMs)
                            ? DateTimeOffset.FromUnixTimeMilliseconds(dMs).UtcDateTime
                            : DateTime.MinValue));
                }
            }

            _logger.LogInformation("Trendyol GetAdPerformance: {Count} metrics for CampaignId={CampaignId}", metrics.Count, campaignId);
            return metrics;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Trendyol GetAdPerformance exception: CampaignId={CampaignId}", campaignId);
            return Array.Empty<TrendyolAdPerformanceDto>();
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

    // ═══════════════════════════════════════════
    // Batch Request ID Logging
    // ═══════════════════════════════════════════

    private void LogBatchRequestId(HttpResponseMessage response, string operation, string identifier, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(body);
                var batchId = doc.RootElement.TryGetProperty("batchRequestId", out var bid) ? bid.GetString() : null;
                if (!string.IsNullOrEmpty(batchId))
                    _logger.LogInformation("Trendyol {Operation} batch accepted: {Identifier} BatchId={BatchId}",
                        operation, identifier, batchId);
            }
            catch { /* non-critical logging */ }
        }, ct);
    }

    // ═══════════════════════════════════════════
    // Trendyol VAT Rate Mapping
    // ═══════════════════════════════════════════

    /// <summary>
    /// Maps product tax rate (decimal, e.g. 0.20) to Trendyol valid VAT rates: 0, 1, 10, 20.
    /// Trendyol API rejects any other value with 400 Bad Request.
    /// </summary>
    private static int MapToTrendyolVatRate(decimal taxRate)
    {
        var pct = (int)Math.Round(taxRate * 100);
        return pct switch
        {
            0 => 0,
            1 => 1,
            10 => 10,
            20 => 20,
            8 => 10,   // 8% → round up to 10% (Trendyol nearest tier)
            18 => 20,  // 18% → round up to 20% (Trendyol nearest tier)
            _ => 20    // Default to highest tier (safe — overcharge not undercharge)
        };
    }

    // ═══════════════════════════════════════════
    // Barcode Resolution — ProductPlatformMapping
    // ═══════════════════════════════════════════

    /// <summary>
    /// Resolves Trendyol barcode from ProductPlatformMapping.ExternalProductId.
    /// Falls back to Product.Barcode or Product.SKU if no mapping exists.
    /// Uses IServiceScopeFactory to access scoped repository from singleton-compatible adapter.
    /// </summary>
    private async Task<string?> ResolveBarcodeAsync(Guid productId, CancellationToken ct)
    {
        if (_scopeFactory is null)
        {
            _logger.LogWarning("TrendyolAdapter: IServiceScopeFactory not available — cannot resolve barcode for {ProductId}. " +
                "Falling back to Guid (will fail on Trendyol API).", productId);
            return productId.ToString();
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var mappingRepo = scope.ServiceProvider.GetService<IProductPlatformMappingRepository>();
            if (mappingRepo is null)
            {
                _logger.LogWarning("TrendyolAdapter: IProductPlatformMappingRepository not registered in DI");
                return null;
            }

            var mappings = await mappingRepo.GetByProductIdAsync(productId, ct).ConfigureAwait(false);
            var trendyolMapping = mappings.FirstOrDefault(m =>
                m.PlatformType == PlatformType.Trendyol && m.IsEnabled);

            if (trendyolMapping is not null && !string.IsNullOrEmpty(trendyolMapping.ExternalProductId))
            {
                _logger.LogDebug("Resolved barcode for {ProductId}: {Barcode} (from PlatformMapping)",
                    productId, trendyolMapping.ExternalProductId);
                return trendyolMapping.ExternalProductId;
            }

            // Fallback: try Product.Barcode or SKU
            var productRepo = scope.ServiceProvider.GetService<IProductRepository>();
            if (productRepo is not null)
            {
                var product = await productRepo.GetByIdAsync(productId, ct).ConfigureAwait(false);
                if (product is not null)
                {
                    var barcode = product.Barcode ?? product.SKU;
                    if (!string.IsNullOrEmpty(barcode))
                    {
                        _logger.LogDebug("Resolved barcode for {ProductId}: {Barcode} (from Product entity)",
                            productId, barcode);
                        return barcode;
                    }
                }
            }

            _logger.LogWarning("TrendyolAdapter: no barcode found for ProductId={ProductId} — " +
                "no PlatformMapping and no Product.Barcode/SKU", productId);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "TrendyolAdapter: barcode resolution failed for ProductId={ProductId}", productId);
            return null;
        }
    }

    /// <summary>
    /// Trendyol JSON product item → Product entity mapping.
    /// D12-11: PullProductsInternalAsync ve SyncProductsDeltaAsync ortak kullanir.
    /// </summary>
    private Product MapJsonToProduct(JsonElement item)
    {
        // Images
        string? imageUrl = null;
        if (item.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array)
        {
            foreach (var img in images.EnumerateArray())
            {
                if (img.ValueKind == JsonValueKind.Object && img.TryGetProperty("url", out var imgUrl))
                {
                    imageUrl ??= imgUrl.GetString();
                    if (imageUrl is not null) break;
                }
            }
        }

        // SKU resolution: stockCode → productMainId → barcode
        var skuValue = item.TryGetProperty("stockCode", out var sc) && sc.ValueKind == JsonValueKind.String ? sc.GetString() : null;
        if (string.IsNullOrEmpty(skuValue))
            skuValue = item.TryGetProperty("productMainId", out var pmi2) && pmi2.ValueKind == JsonValueKind.String ? pmi2.GetString() : null;
        if (string.IsNullOrEmpty(skuValue))
            skuValue = item.TryGetProperty("barcode", out var bc2) ? bc2.GetString() : null;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
            SKU = skuValue ?? "",
            Barcode = item.TryGetProperty("barcode", out var b) ? b.GetString() : null,
            SalePrice = item.TryGetProperty("salePrice", out var sp) ? sp.GetDecimal() : 0,
            ListPrice = item.TryGetProperty("listPrice", out var lp) ? lp.GetDecimal() : null,
            Description = item.TryGetProperty("description", out var d) ? d.GetString() : null,
            TaxRate = item.TryGetProperty("vatRate", out var vr) ? vr.GetDecimal() / 100m : 0.18m,
            ImageUrl = imageUrl,
            Code = item.TryGetProperty("productMainId", out var pmi) ? pmi.GetString() : null,
            CurrencyCode = item.TryGetProperty("currencyType", out var ccy) ? ccy.GetString() ?? "TRY" : "TRY"
        };
        product.SyncStock(item.TryGetProperty("quantity", out var q) ? q.GetInt32() : 0, "trendyol-delta-sync");

        return product;
    }

    /// <summary>
    /// Line-level SKU resolution: merchantSku → stockCode → barcode.
    /// Guard: stockCode="merchantSku" literal string → placeholder, barcode'a fallback.
    /// </summary>
    private static string? ResolveLineSku(JsonElement line)
    {
        // 1. merchantSku (Trendyol bazen null döndürür)
        if (line.TryGetProperty("merchantSku", out var msku) && msku.ValueKind == JsonValueKind.String)
        {
            var val = msku.GetString();
            if (!string.IsNullOrEmpty(val)) return val;
        }

        // 2. stockCode — ama "merchantSku" literal string'i placeholder'dır, kullanma
        if (line.TryGetProperty("stockCode", out var sc) && sc.ValueKind == JsonValueKind.String)
        {
            var val = sc.GetString();
            if (!string.IsNullOrEmpty(val) &&
                !string.Equals(val, "merchantSku", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(val, "stockCode", StringComparison.OrdinalIgnoreCase))
                return val;
        }

        // 3. barcode fallback
        if (line.TryGetProperty("barcode", out var bc) && bc.ValueKind == JsonValueKind.String)
            return bc.GetString();

        return null;
    }
}
