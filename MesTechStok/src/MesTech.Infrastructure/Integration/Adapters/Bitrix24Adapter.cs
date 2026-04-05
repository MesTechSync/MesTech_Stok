using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Bitrix24 CRM adapter — deal push, contact sync, catalog management.
/// OAuth2 Authorization Code flow via Bitrix24AuthProvider.
/// API base: https://{portal}.bitrix24.com/rest/
/// Rate limit: SemaphoreSlim configurable (Enterprise 50, Free 2).
/// Batch API: 50 commands/request chunking.
/// </summary>
public sealed class Bitrix24Adapter : IBitrix24Adapter, IWebhookCapableAdapter, IPingableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<Bitrix24Adapter> _logger;
    private readonly Bitrix24Options _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static SemaphoreSlim _rateLimitSemaphore = new(50, 50);
    private const int MaxBatchCommands = 50;

    private Bitrix24AuthProvider? _authProvider;
    private string _portalDomain = string.Empty;
    private bool _isConfigured;

    public Bitrix24Adapter(HttpClient httpClient, ILogger<Bitrix24Adapter> logger,
        IHttpClientFactory? httpClientFactory = null, IOptions<Bitrix24Options>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClientFactory = httpClientFactory;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new Bitrix24Options();
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);

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
                DelayGenerator = args =>
                {
                    // Respect Retry-After header on 429
                    if (args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests } response
                        && response.Headers.RetryAfter?.Delta is { } retryAfter)
                    {
                        return new ValueTask<TimeSpan?>(retryAfter);
                    }
                    return new ValueTask<TimeSpan?>(
                        TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber)));
                },
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
                    .HandleResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Bitrix24 API retry {Attempt} after {Delay}ms (status: {Status})",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Result?.StatusCode);
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
                    _logger.LogWarning("Bitrix24 circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public string PlatformCode => nameof(PlatformType.Bitrix24);
    public bool SupportsStockUpdate => false;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => false;

    /// <summary>
    /// Configure rate limit concurrency at runtime (Enterprise=50, Free=2).
    /// Thread-safe: uses Interlocked.Exchange. Old semaphore is NOT disposed immediately
    /// to avoid ObjectDisposedException on threads still holding a reference.
    /// GC/finalizer will reclaim the old instance after all waiters complete.
    /// </summary>
    public static void ConfigureRateLimit(int maxConcurrency)
    {
        // Do NOT dispose the old semaphore — concurrent threads may still hold a reference
        // from ApplyRateLimitAsync. Disposing it causes ObjectDisposedException (TOCTOU race).
        _ = Interlocked.Exchange(
            ref _rateLimitSemaphore, new SemaphoreSlim(maxConcurrency, maxConcurrency));
    }

    #region IIntegratorAdapter — Core Methods

    private void ConfigureAuth(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var clientId = credentials.GetValueOrDefault("Bitrix24ClientId", "");
        var clientSecret = credentials.GetValueOrDefault("Bitrix24ClientSecret", "");
        _portalDomain = credentials.GetValueOrDefault("Bitrix24PortalDomain", "");
        var refreshToken = credentials.GetValueOrDefault("Bitrix24RefreshToken", "");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(_portalDomain) || string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException(
                "Bitrix24 credentials require: Bitrix24ClientId, Bitrix24ClientSecret, Bitrix24PortalDomain, Bitrix24RefreshToken");
        }

        // SSRF guard (G10853)
        if (Security.SsrfGuard.IsPrivateHost(_portalDomain))
            _logger.LogWarning("[Bitrix24Adapter] PortalDomain points to private network: {Domain}", _portalDomain);

        _httpClient.BaseAddress ??= new Uri($"https://{_portalDomain}/rest/", UriKind.Absolute);

        var tokenEndpoint = credentials.GetValueOrDefault("Bitrix24TokenEndpoint");

        _authProvider = CreateAuthProvider();
        _authProvider.Configure(clientId, clientSecret, _portalDomain, refreshToken, tokenEndpoint);
        _isConfigured = true;

        _logger.LogInformation("Bitrix24Adapter configured for portal {Portal}", _portalDomain);
    }

    private Bitrix24AuthProvider CreateAuthProvider()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var authHttpClient = _httpClientFactory?.CreateClient("Bitrix24Auth")
            ?? throw new InvalidOperationException("IHttpClientFactory is required for Bitrix24Auth client creation");
        return new Bitrix24AuthProvider(
            authHttpClient,
            new InMemoryTokenCacheProvider(),
            loggerFactory.CreateLogger<Bitrix24AuthProvider>());
    }

    public async Task<ConnectionTestResultDto> TestConnectionAsync(
        Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var sw = Stopwatch.StartNew();
        var result = new ConnectionTestResultDto { PlatformCode = PlatformCode };

        try
        {
            var requiredKeys = new[] { "Bitrix24ClientId", "Bitrix24ClientSecret", "Bitrix24PortalDomain", "Bitrix24RefreshToken" };
            foreach (var key in requiredKeys)
            {
                if (!credentials.ContainsKey(key) || string.IsNullOrWhiteSpace(credentials[key]))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Missing required credential: {key}";
                    result.ResponseTime = sw.Elapsed;
                    return result;
                }
            }

            ConfigureAuth(credentials);
            await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

            // Lightweight test: get current user info
            using var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, "profile"), ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);

                result.IsSuccess = true;
                result.StoreName = _portalDomain;
                result.ProductCount = 0;

                if (doc.RootElement.TryGetProperty("result", out var resultObj) &&
                    resultObj.TryGetProperty("NAME", out var name))
                {
                    result.StoreName = $"{_portalDomain} ({name.GetString()})";
                }
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.IsSuccess = false;
                result.ErrorMessage = $"Bitrix24 API returned {response.StatusCode}: {errorBody}";
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"Bitrix24 connection failed: {ex.Message}";
            _logger.LogError(ex, "Bitrix24 TestConnectionAsync failed for {Portal}", _portalDomain);
        }

        result.ResponseTime = sw.Elapsed;
        return result;
    }

    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        try
        {
            var fields = new Dictionary<string, object>
            {
                ["NAME"] = product.Name,
                ["CURRENCY_ID"] = "TRY",
                ["PRICE"] = product.SalePrice,
                ["DESCRIPTION"] = product.Description ?? ""
            };

            var content = new FormUrlEncodedContent(
                fields.Select(kvp => new KeyValuePair<string, string>($"fields[{kvp.Key}]", kvp.Value.ToString()!)));

            using var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Post, "crm.product.add") { Content = content },
                ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Bitrix24 product pushed: {Name}", product.Name);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Bitrix24 PushProduct failed: {Status} {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 PushProductAsync failed for {Name}", product.Name);
            return false;
        }
    }

    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        var products = new List<Product>();
        var start = 0;

        try
        {
            while (true)
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["start"] = start.ToString(),
                    ["select[0]"] = "ID",
                    ["select[1]"] = "NAME",
                    ["select[2]"] = "PRICE",
                    ["select[3]"] = "CURRENCY_ID",
                    ["select[4]"] = "DESCRIPTION"
                });

                using var response = await ExecuteWithRetryAsync(
                    () => new HttpRequestMessage(HttpMethod.Post, "crm.product.list") { Content = content },
                    ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    break;
                }

                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("result", out var resultArray)) break;

                foreach (var item in resultArray.EnumerateArray())
                {
                    products.Add(new Product
                    {
                        Name = item.TryGetProperty("NAME", out var n) ? n.GetString() ?? "" : "",
                        SalePrice = item.TryGetProperty("PRICE", out var p) && decimal.TryParse(p.GetString(), out var price)
                            ? price : 0m,
                        Description = item.TryGetProperty("DESCRIPTION", out var d) ? d.GetString() : null
                    });
                }

                // Bitrix24 pagination: "next" field indicates more pages
                if (!doc.RootElement.TryGetProperty("next", out var next)) break;
                start = next.GetInt32();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 PullProductsAsync failed");
        }

        _logger.LogInformation("Bitrix24 pulled {Count} products", products.Count);
        return products;
    }

    public async Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        // Bitrix24 CRM does not have native stock management
        // SupportsStockUpdate = false — this method returns false
        _logger.LogWarning("Bitrix24 does not support stock updates (CRM-focused)");
        return await Task.FromResult(false).ConfigureAwait(false);
    }

    public async Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        try
        {
            // Find product by external ID (would need platform mapping)
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["id"] = productId.ToString(),
                ["fields[PRICE]"] = newPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
            });

            using var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Post, "crm.product.update") { Content = content },
                ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Bitrix24 PushPriceUpdate failed: {Status} {Error}", response.StatusCode, errorBody);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 PushPriceUpdateAsync failed for {ProductId}", productId);
            return false;
        }
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        var categories = new List<CategoryDto>();

        try
        {
            using var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Post, "catalog.section.list"),
                ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return categories;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("result", out var resultObj) &&
                resultObj.TryGetProperty("sections", out var sections))
            {
                foreach (var s in sections.EnumerateArray())
                {
                    categories.Add(new CategoryDto
                    {
                        PlatformCategoryId = s.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out var catId) ? catId : 0,
                        Name = s.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                        ParentId = s.TryGetProperty("sectionId", out var pid) && pid.ValueKind == JsonValueKind.Number && pid.TryGetInt32(out var parentId) ? parentId : null
                    });
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 GetCategoriesAsync failed");
        }

        return categories;
    }

    #endregion

    #region IBitrix24Adapter — CRM Methods

    public async Task<string?> PushDealAsync(Order order, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        try
        {
            var fields = new Dictionary<string, string>
            {
                ["fields[TITLE]"] = $"MesTech Order #{order.OrderNumber}",
                ["fields[OPPORTUNITY]"] = order.TotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                ["fields[CURRENCY_ID]"] = "TRY",
                ["fields[STAGE_ID]"] = MapOrderStatusToStage(order.Status),
                ["fields[UF_MESTECH_ORDER_ID]"] = order.Id.ToString(),
                ["fields[BEGINDATE]"] = order.OrderDate.ToString("yyyy-MM-ddTHH:mm:ss")
            };

            // Link contact if customer is synced
            if (order.CustomerId != Guid.Empty)
                fields["fields[CONTACT_ID]"] = order.CustomerId.ToString();

            var content = new FormUrlEncodedContent(fields);

            using var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Post, "crm.deal.add") { Content = content },
                ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Bitrix24 PushDeal failed: {Status} {Error}", response.StatusCode, errorBody);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("result", out var dealId))
                return null;

            var externalDealId = dealId.ToString();
            _logger.LogInformation("Bitrix24 deal created: {DealId} for order {OrderId}",
                externalDealId, order.Id);

            // Set product rows if order has items
            if (order.OrderItems?.Any() == true)
                await SetDealProductRowsAsync(externalDealId, order.OrderItems, ct).ConfigureAwait(false);

            return externalDealId;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 PushDealAsync failed for order {OrderId}", order.Id);
            return null;
        }
    }

    public async Task<int> SyncContactsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        // Contact sync requires customer list from MesTech domain — this method
        // is called by CQRS handler which passes context. Here we fetch existing
        // Bitrix24 contacts to enable incremental sync.
        try
        {
            using var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Post, "crm.contact.list"),
                ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return 0;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("result", out var contacts)) return 0;

            var count = contacts.GetArrayLength();
            _logger.LogInformation("Bitrix24 SyncContacts: {Count} contacts found", count);
            return count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 SyncContactsAsync failed");
            return 0;
        }
    }

    public async Task<IReadOnlyList<Product>> GetCatalogProductsAsync(CancellationToken ct = default)
    {
        // Delegates to PullProductsAsync — catalog.product.list is the same as crm.product.list
        return await PullProductsAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> UpdateDealStageAsync(string externalDealId, string stageId, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["id"] = externalDealId,
                ["fields[STAGE_ID]"] = stageId
            });

            using var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Post, "crm.deal.update") { Content = content },
                ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Bitrix24 deal {DealId} stage updated to {Stage}",
                    externalDealId, stageId);
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Bitrix24 UpdateDealStage failed: {Status} {Error}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 UpdateDealStageAsync failed for deal {DealId}", externalDealId);
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> BatchExecuteAsync(
        IReadOnlyList<string> commands, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        var allResults = new List<string>();

        // Chunk into groups of MaxBatchCommands (50)
        var chunks = commands
            .Select((cmd, i) => new { cmd, i })
            .GroupBy(x => x.i / MaxBatchCommands)
            .Select(g => g.Select(x => x.cmd).ToList())
            .ToList();

        foreach (var chunk in chunks)
        {
            try
            {
                var formFields = new List<KeyValuePair<string, string>>();
                for (var i = 0; i < chunk.Count; i++)
                {
                    formFields.Add(new KeyValuePair<string, string>($"cmd[cmd{i}]", chunk[i]));
                }

                var content = new FormUrlEncodedContent(formFields);

                using var response = await ExecuteWithRetryAsync(
                    () => new HttpRequestMessage(HttpMethod.Post, "batch") { Content = content },
                    ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("Bitrix24 batch request failed: {Status} {Error}", response.StatusCode, errorBody);
                    // Add empty results for this chunk
                    allResults.AddRange(chunk.Select(_ => ""));
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("result", out var batchResult))
                {
                    // Extract individual results
                    if (batchResult.TryGetProperty("result", out var results))
                    {
                        foreach (var prop in results.EnumerateObject())
                        {
                            allResults.Add(prop.Value.GetRawText());
                        }
                    }

                    // Log partial failures
                    if (batchResult.TryGetProperty("result_error", out var errors))
                    {
                        foreach (var err in errors.EnumerateObject())
                        {
                            _logger.LogWarning(
                                "Bitrix24 batch partial failure: {Command} — {Error}",
                                err.Name, err.Value.GetRawText());
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Bitrix24 batch chunk failed ({Count} commands)", chunk.Count);
                allResults.AddRange(chunk.Select(_ => ""));
            }
        }

        _logger.LogInformation("Bitrix24 batch: {Total} commands, {Chunks} chunks, {Results} results",
            commands.Count, chunks.Count, allResults.Count);

        return allResults;
    }

    #endregion

    #region IWebhookCapableAdapter

    public async Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        try
        {
            // Bitrix24 event.bind — register webhook for CRM events
            var events = new[] { "ONCRMDEALADD", "ONCRMDEALUPDATE", "ONCRMCONTACTADD", "ONCRMCONTACTUPDATE" };

            foreach (var eventName in events)
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["event"] = eventName,
                    ["handler"] = callbackUrl
                });

                using var response = await ExecuteWithRetryAsync(
                    () => new HttpRequestMessage(HttpMethod.Post, "event.bind") { Content = content },
                    ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("Bitrix24 webhook bind failed for {Event}: {Status} {Error}",
                        eventName, response.StatusCode, errorBody);
                }
            }

            _logger.LogInformation("Bitrix24 webhooks registered for {Url}", callbackUrl);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 RegisterWebhookAsync failed");
            return false;
        }
    }

    // IWebhookCapableAdapter explicit implementation (no callbackUrl)
    async Task<bool> IWebhookCapableAdapter.UnregisterWebhookAsync(CancellationToken ct)
        => await UnregisterWebhookAsync(string.Empty, ct).ConfigureAwait(false);

    public async Task<bool> UnregisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        try
        {
            var events = new[] { "ONCRMDEALADD", "ONCRMDEALUPDATE", "ONCRMCONTACTADD", "ONCRMCONTACTUPDATE" };

            foreach (var eventName in events)
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["event"] = eventName,
                    ["handler"] = callbackUrl
                });

                await ExecuteWithRetryAsync(
                    () => new HttpRequestMessage(HttpMethod.Post, "event.unbind") { Content = content },
                    ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Bitrix24 webhooks unregistered for {Url}", callbackUrl);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 UnregisterWebhookAsync failed");
            return false;
        }
    }

    // IWebhookCapableAdapter explicit implementation (no signature, returns Task)
    async Task IWebhookCapableAdapter.ProcessWebhookPayloadAsync(string payload, CancellationToken ct)
        => await ProcessWebhookPayloadAsync(payload, null, ct).ConfigureAwait(false);

    public Task<bool> ProcessWebhookPayloadAsync(string payload, string? signature, CancellationToken ct = default)
    {
        try
        {
            // Bitrix24 webhooks send form-encoded data with auth[application_token]
            // Validation is done by comparing application_token with stored value
            // The actual processing is delegated to WebhookReceiverService

            _logger.LogInformation("Bitrix24 webhook payload received ({Length} bytes)", payload?.Length ?? 0);
            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 ProcessWebhookPayloadAsync failed");
            return Task.FromResult(false);
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Push a single contact to Bitrix24 CRM (crm.contact.add).
    /// </summary>
    public async Task<string?> PushContactAsync(Customer customer, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        try
        {
            var nameParts = (customer.Name ?? "").Split(' ', 2);
            var fields = new Dictionary<string, string>
            {
                ["fields[NAME]"] = nameParts.Length > 0 ? nameParts[0] : "",
                ["fields[LAST_NAME]"] = nameParts.Length > 1 ? nameParts[1] : "",
                ["fields[PHONE][0][VALUE]"] = customer.Phone ?? "",
                ["fields[PHONE][0][VALUE_TYPE]"] = "WORK",
                ["fields[EMAIL][0][VALUE]"] = customer.Email ?? "",
                ["fields[EMAIL][0][VALUE_TYPE]"] = "WORK",
                ["fields[ADDRESS_CITY]"] = customer.City ?? "",
                ["fields[UF_MESTECH_CUSTOMER_ID]"] = customer.Id.ToString()
            };

            if (!string.IsNullOrEmpty(customer.Mobile))
            {
                fields["fields[PHONE][1][VALUE]"] = customer.Mobile;
                fields["fields[PHONE][1][VALUE_TYPE]"] = "MOBILE";
            }

            var content = new FormUrlEncodedContent(fields);

            using var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Post, "crm.contact.add") { Content = content },
                ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("result", out var contactId))
            {
                _logger.LogInformation("Bitrix24 contact created: {ContactId} for customer {CustomerId}",
                    contactId, customer.Id);
                return contactId.ToString();
            }

            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 PushContactAsync failed for customer {CustomerId}", customer.Id);
            return null;
        }
    }

    /// <summary>
    /// Update an existing Bitrix24 contact (crm.contact.update).
    /// </summary>
    public async Task<bool> UpdateContactAsync(string externalContactId, Customer customer, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureAuthHeaderAsync(ct).ConfigureAwait(false);

        try
        {
            var nameParts = (customer.Name ?? "").Split(' ', 2);
            var fields = new Dictionary<string, string>
            {
                ["id"] = externalContactId,
                ["fields[NAME]"] = nameParts.Length > 0 ? nameParts[0] : "",
                ["fields[LAST_NAME]"] = nameParts.Length > 1 ? nameParts[1] : "",
                ["fields[PHONE][0][VALUE]"] = customer.Phone ?? "",
                ["fields[PHONE][0][VALUE_TYPE]"] = "WORK",
                ["fields[EMAIL][0][VALUE]"] = customer.Email ?? "",
                ["fields[EMAIL][0][VALUE_TYPE]"] = "WORK"
            };

            var content = new FormUrlEncodedContent(fields);

            using var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Post, "crm.contact.update") { Content = content },
                ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Bitrix24 UpdateContact failed: {Status} {Error}", response.StatusCode, errorBody);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Bitrix24 UpdateContactAsync failed for {ContactId}", externalContactId);
            return false;
        }
    }

    private async Task SetDealProductRowsAsync(
        string dealId, IEnumerable<OrderItem> orderItems, CancellationToken ct)
    {
        var formFields = new List<KeyValuePair<string, string>>
        {
            new("id", dealId)
        };

        var i = 0;
        foreach (var item in orderItems)
        {
            formFields.Add(new($"rows[{i}][PRODUCT_NAME]", item.ProductName));
            formFields.Add(new($"rows[{i}][QUANTITY]", item.Quantity.ToString()));
            formFields.Add(new($"rows[{i}][PRICE]", item.UnitPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)));
            formFields.Add(new($"rows[{i}][TAX_RATE]", item.TaxRate.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)));
            i++;
        }

        var content = new FormUrlEncodedContent(formFields);

        using var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "crm.deal.productrows.set") { Content = content },
            ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Bitrix24 SetDealProductRows failed for deal {DealId}: {Status} {Error}",
                dealId, response.StatusCode, errorBody);
        }
    }

    private static string MapOrderStatusToStage(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "NEW",
        OrderStatus.Confirmed => "PREPARATION",
        OrderStatus.Shipped => "EXECUTING",
        OrderStatus.Delivered => "WON",
        OrderStatus.Cancelled => "LOSE",
        _ => "NEW"
    };

    private async Task<string> GetAuthTokenAsync(CancellationToken ct)
    {
        if (_authProvider is null)
            throw new InvalidOperationException("Bitrix24Adapter not configured. Call TestConnectionAsync first.");

        var token = await _authProvider.GetTokenAsync(ct).ConfigureAwait(false);
        return token.AccessToken;
    }

    private async Task EnsureAuthHeaderAsync(CancellationToken ct)
    {
        var token = await GetAuthTokenAsync(ct).ConfigureAwait(false);
        // Per-request auth is now handled in ExecuteWithRetryAsync.
        // This method is kept for backward compat but is a no-op.
        _ = token;
    }

    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        var authToken = await GetAuthTokenAsync(ct).ConfigureAwait(false);
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _retryPipeline.ExecuteAsync(async token =>
            {
                using var request = requestFactory();
                request.Headers.TryAddWithoutValidation("User-Agent", "MesTech-Bitrix24-Client/1.0");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
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

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "Bitrix24Adapter not configured. Call TestConnectionAsync first.");
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            if (string.IsNullOrEmpty(_portalDomain))
            {
                _logger.LogDebug("Bitrix24 ping skipped — not configured");
                return false;
            }

            var request = new HttpRequestMessage(HttpMethod.Head,
                new Uri($"https://{_portalDomain}/rest/", UriKind.Absolute));
            using var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);

            _logger.LogDebug("Bitrix24 ping: {StatusCode}", response.StatusCode);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Bitrix24 ping failed");
            return false;
        }
    }

    #endregion
}
