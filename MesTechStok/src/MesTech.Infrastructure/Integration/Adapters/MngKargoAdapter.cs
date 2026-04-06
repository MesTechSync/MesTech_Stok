using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Cargo;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Cargo;
using MesTech.Infrastructure.Integration.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// MNG Kargo REST adaptoru.
/// API Key + Secret auth (HMAC-SHA256 signature), JSON payloads, Polly retry + circuit breaker.
/// Base URL: https://apizone.mngkargo.com.tr/ (configurable via credentials["BaseUrl"])
/// Note: Endpoint paths, auth scheme, and field names are provisional — confirm with MNG Kargo API documentation.
/// </summary>
public sealed class MngKargoAdapter : ICargoAdapter, ICargoRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MngKargoAdapter> _logger;
    private readonly MngKargoOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);

    private string _apiKey = string.Empty;
    private string _apiSecret = string.Empty;
    private string _basicAuthValue = string.Empty;
    private string _customerCode = string.Empty;
    private bool _isConfigured;

    public MngKargoAdapter(HttpClient httpClient, ILogger<MngKargoAdapter> logger,
        IOptions<MngKargoOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? new MngKargoOptions();
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            // HTTP 429 rate-limit retry
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                DelayGenerator = args =>
                {
                    if (args.Outcome.Result is { StatusCode: System.Net.HttpStatusCode.TooManyRequests } resp
                        && resp.Headers.RetryAfter is { } ra)
                        return new ValueTask<TimeSpan?>(ra.Delta ?? TimeSpan.FromSeconds(3));
                    return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(3));
                },
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests),
                OnRetry = args =>
                {
                    _logger.LogWarning("[MngKargo] Rate limited (429). Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("MNG Kargo API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("MngKargo circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public CargoProvider Provider => CargoProvider.MngKargo;
    public bool SupportsCancellation => true;
    public bool SupportsLabelGeneration => true;
    public bool SupportsCashOnDelivery => true;
    public bool SupportsMultiParcel => true;

    public void Configure(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        _apiKey = credentials.GetValueOrDefault("ApiKey", "");
        _apiSecret = credentials.GetValueOrDefault("ApiSecret", "");
        _customerCode = credentials.GetValueOrDefault("CustomerCode", "");

        // Store encoded credentials for per-request auth (thread-safe)
        _basicAuthValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiKey}:{_apiSecret}"));

        var baseUrl = credentials.GetValueOrDefault("BaseUrl", _options.BaseUrl);
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != "https" && parsedUri.Scheme != "http"))
            throw new ArgumentException($"Invalid MngKargo base URL scheme: {baseUrl}. Only HTTP(S) allowed.");
        if (SsrfGuard.IsPrivateHost(parsedUri.Host))
            _logger.LogWarning("[MngKargoAdapter] BaseUrl points to private network: {BaseUrl}", baseUrl);
        _httpClient.BaseAddress = parsedUri;

        _isConfigured = true;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException("MngKargoAdapter henuz konfigure edilmedi.");
    }

    // -- ICargoAdapter.IsAvailableAsync ----------------------
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_isConfigured) return false;
        try
        {
            using var response = await ExecuteWithRetryAsync(
                () => CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/health"), ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "[MngKargoAdapter] IsAvailableAsync health check failed");
            return false;
        }
    }

    // -- CreateShipmentAsync ---------------------------------
    public async Task<ShipmentResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var payload = new
            {
                customerCode = _customerCode,
                referenceNo = request.OrderId.ToString(),
                receiverName = request.RecipientName,
                receiverPhone = request.RecipientPhone,
                receiverAddress = request.RecipientAddress.FullAddress,
                receiverCity = request.RecipientAddress.City,
                receiverDistrict = request.RecipientAddress.District,
                receiverPostalCode = request.RecipientAddress.PostalCode,
                senderAddress = request.SenderAddress.FullAddress,
                senderCity = request.SenderAddress.City,
                weight = request.Weight,
                desi = request.Desi,
                parcelCount = request.ParcelCount,
                codAmount = request.CodAmount,
                notes = request.Notes
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var response = await ExecuteWithRetryAsync(() =>
            {
                var req = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/shipments");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("MNG Kargo CreateShipment failed {Status}: {Error}",
                    response.StatusCode, error);
                return ShipmentResult.Failed($"MNG Kargo hatasi: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);
            var trackingNo = doc.RootElement.TryGetProperty("trackingNumber", out var tn)
                ? tn.GetString()
                : doc.RootElement.TryGetProperty("barcode", out var bc)
                    ? bc.GetString() : null;
            var shipmentId = doc.RootElement.TryGetProperty("shipmentId", out var si)
                ? si.GetString() : null;

            if (string.IsNullOrEmpty(trackingNo))
                return ShipmentResult.Failed("MNG Kargo tracking number alinamadi");

            _logger.LogInformation("MNG Kargo shipment created: {TrackingNo}", trackingNo);
            return ShipmentResult.Succeeded(trackingNo, shipmentId ?? trackingNo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "MNG Kargo CreateShipment failed for order {OrderId}", request.OrderId);
            return ShipmentResult.Failed($"MNG Kargo hatasi: {ex.Message}");
        }
    }

    // -- TrackShipmentAsync ----------------------------------
    public async Task<TrackingResult> TrackShipmentAsync(string trackingNumber, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        EnsureConfigured();
        var trackingResult = new TrackingResult { TrackingNumber = trackingNumber };

        try
        {
            using var response = await ExecuteWithRetryAsync(
                () => CreateAuthenticatedRequest(HttpMethod.Get,
                    $"/api/v1/tracking/{trackingNumber}"), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("MNG Kargo tracking failed {Status} {Error}", response.StatusCode, errorBody);
                return trackingResult;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("status", out var statusProp))
                trackingResult.Status = MapMngStatus(statusProp.GetString() ?? "");

            if (doc.RootElement.TryGetProperty("estimatedDelivery", out var eta) &&
                DateTime.TryParse(eta.GetString(), out var etaDate))
                trackingResult.EstimatedDelivery = etaDate;

            if (doc.RootElement.TryGetProperty("events", out var events))
            {
                foreach (var evt in events.EnumerateArray())
                {
                    trackingResult.Events.Add(new TrackingEvent
                    {
                        Timestamp = evt.TryGetProperty("timestamp", out var tsProp)
                            && DateTime.TryParse(tsProp.GetString(), out var ts)
                            ? ts : DateTime.UtcNow,
                        Location = evt.TryGetProperty("location", out var loc)
                            ? loc.GetString() ?? "" : "",
                        Description = evt.TryGetProperty("description", out var desc)
                            ? desc.GetString() ?? "" : "",
                        Status = evt.TryGetProperty("status", out var st)
                            ? MapMngStatus(st.GetString() ?? "") : CargoStatus.Created
                    });
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "MNG Kargo tracking failed for {TrackingNumber}", trackingNumber);
            trackingResult.Status = CargoStatus.Created;
        }

        return trackingResult;
    }

    // -- CancelShipmentAsync ---------------------------------
    public async Task<bool> CancelShipmentAsync(string shipmentId, CancellationToken ct = default)
    {
        EnsureConfigured();

        using var response = await ExecuteWithRetryAsync(
            () => CreateAuthenticatedRequest(HttpMethod.Delete,
                $"/api/v1/shipments/{shipmentId}"), ct).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("MNG Kargo shipment cancelled: {ShipmentId}", shipmentId);
            return true;
        }

        _logger.LogWarning("MNG Kargo cancel failed {Status}", response.StatusCode);
        return false;
    }

    // -- GetShipmentLabelAsync -------------------------------
    public async Task<LabelResult> GetShipmentLabelAsync(string shipmentId, CancellationToken ct = default)
        => await GetShipmentLabelAsync(shipmentId, LabelFormat.Pdf, ct).ConfigureAwait(false);

    public async Task<LabelResult> GetShipmentLabelAsync(string shipmentId, LabelFormat preferredFormat, CancellationToken ct = default)
    {
        EnsureConfigured();

        var formatQuery = preferredFormat switch
        {
            LabelFormat.Zpl => "?format=zpl",
            LabelFormat.Png => "?format=png",
            _ => ""
        };

        using var response = await ExecuteWithRetryAsync(
            () => CreateAuthenticatedRequest(HttpMethod.Get,
                $"/api/v1/shipments/{shipmentId}/label{formatQuery}"), ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new HttpRequestException($"MNG Kargo label request failed: {response.StatusCode} — {errorBody}");
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(content);
        var base64 = doc.RootElement.TryGetProperty("labelData", out var ld)
            ? ld.GetString()
            : doc.RootElement.TryGetProperty("pdfData", out var pd)
                ? pd.GetString() : null;

        if (string.IsNullOrEmpty(base64))
            throw new InvalidOperationException("MNG Kargo etiket verisi bos");

        var ext = preferredFormat switch
        {
            LabelFormat.Zpl => "zpl",
            LabelFormat.Png => "png",
            _ => "pdf"
        };

        return new LabelResult
        {
            Data = Convert.FromBase64String(base64),
            Format = preferredFormat,
            FileName = $"mng-label-{shipmentId}.{ext}"
        };
    }

    // -- Helpers ---------------------------------------------
    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(_basicAuthValue))
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _basicAuthValue);
        if (!string.IsNullOrEmpty(_apiKey))
            request.Headers.TryAddWithoutValidation("X-API-Key", _apiKey);
        request.Headers.TryAddWithoutValidation("User-Agent", "MesTech-MngKargo-Client/1.0");
        return request;
    }

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
            _logger.LogWarning(ex, "MngKargo circuit breaker is open — returning 503");
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

    // ── ICargoRateProvider ─────────────────────────────
    public Task<CargoRateResult?> GetRateAsync(ShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var rate = DesiBasedCargoRateCalculator.Calculate(Provider, request);
        return Task.FromResult<CargoRateResult?>(rate);
    }

    private static CargoStatus MapMngStatus(string status) => status.ToLowerInvariant() switch
    {
        "created" or "olusturuldu" => CargoStatus.Created,
        "pickedup" or "picked_up" or "alindi" => CargoStatus.PickedUp,
        "intransit" or "in_transit" or "yolda" or "transferde" => CargoStatus.InTransit,
        "outfordelivery" or "out_for_delivery" or "dagitimda" => CargoStatus.OutForDelivery,
        "delivered" or "teslim_edildi" => CargoStatus.Delivered,
        "returned" or "iade" or "iade_edildi" => CargoStatus.Returned,
        "cancelled" or "iptal" or "iptal_edildi" => CargoStatus.Cancelled,
        "atbranch" or "at_branch" or "subede" => CargoStatus.AtBranch,
        "lost" or "kayip" => CargoStatus.Lost,
        _ => CargoStatus.Created
    };
}
