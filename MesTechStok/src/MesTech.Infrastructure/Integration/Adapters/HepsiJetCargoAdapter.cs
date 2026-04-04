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
/// HepsiJet Kargo REST adaptoru (Hepsiburada kargo kolu).
/// OAuth2-like Bearer token auth (username/password → token), JSON payloads,
/// Polly retry + circuit breaker.
/// </summary>
public sealed class HepsiJetCargoAdapter : ICargoAdapter, ICargoRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HepsiJetCargoAdapter> _logger;
    private readonly HepsiJetCargoOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _customerCode = string.Empty;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private bool _isConfigured;

    public HepsiJetCargoAdapter(HttpClient httpClient, ILogger<HepsiJetCargoAdapter> logger,
        IOptions<HepsiJetCargoOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? new HepsiJetCargoOptions();
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
                    _logger.LogWarning("[HepsiJetCargo] Rate limited (429). Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("HepsiJet API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("HepsiJet circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public CargoProvider Provider => CargoProvider.Hepsijet;
    public bool SupportsCancellation => false;
    public bool SupportsLabelGeneration => true;
    public bool SupportsCashOnDelivery => true;
    public bool SupportsMultiParcel => true;

    public void Configure(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        _username = credentials.GetValueOrDefault("UserName", "");
        _password = credentials.GetValueOrDefault("Password", "");
        _customerCode = credentials.GetValueOrDefault("CustomerCode", "");

        var rawBaseUrl = credentials.GetValueOrDefault("BaseUrl", "");
        if (!string.IsNullOrEmpty(rawBaseUrl))
        {
            if (!Uri.TryCreate(rawBaseUrl, UriKind.Absolute, out var parsedUri) ||
                (parsedUri.Scheme != "https" && parsedUri.Scheme != "http"))
                throw new ArgumentException($"Invalid HepsiJet base URL scheme: {rawBaseUrl}. Only HTTP(S) allowed.");
            if (SsrfGuard.IsPrivateHost(parsedUri.Host))
                _logger.LogWarning("[HepsiJetCargoAdapter] BaseUrl points to private network: {BaseUrl}", rawBaseUrl);
            _httpClient.BaseAddress = parsedUri;
        }

        _isConfigured = true;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException("HepsiJetCargoAdapter henuz konfigure edilmedi.");
    }

    // -- OAuth2-like Token Management ------------------------
    private async Task EnsureTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            return;

        await _tokenLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
                return;

            var tokenPayload = new
            {
                username = _username,
                password = _password
            };

            var json = JsonSerializer.Serialize(tokenPayload, _jsonOptions);
            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/token")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(tokenRequest, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("HepsiJet token request failed {Status}: {Error}",
                    response.StatusCode, error);
                throw new HttpRequestException($"HepsiJet token request failed: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            _accessToken = doc.RootElement.TryGetProperty("accessToken", out var at)
                ? at.GetString()
                : doc.RootElement.TryGetProperty("token", out var t)
                    ? t.GetString()
                    : null;

            if (string.IsNullOrEmpty(_accessToken))
                throw new InvalidOperationException("HepsiJet token alinamadi");

            // Default 55 minutes expiry (token typically valid for 60 min)
            var expiresIn = doc.RootElement.TryGetProperty("expiresIn", out var exp)
                ? exp.GetInt32()
                : 3300;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

            _logger.LogInformation("HepsiJet token refreshed, expires in {ExpiresIn}s", expiresIn);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    // -- ICargoAdapter.IsAvailableAsync ----------------------
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_isConfigured) return false;
        try
        {
            using var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, "/api/v1/health"), ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[HepsiJetCargoAdapter] IsAvailableAsync health check failed");
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
                referenceId = request.OrderId.ToString(),
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
                var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/shipments");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("HepsiJet CreateShipment failed {Status}: {Error}",
                    response.StatusCode, error);
                return ShipmentResult.Failed($"HepsiJet hatasi: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);
            var trackingNo = doc.RootElement.TryGetProperty("trackingNumber", out var tn)
                ? tn.GetString() : null;
            var shipmentId = doc.RootElement.TryGetProperty("shipmentId", out var si)
                ? si.GetString() : null;

            if (string.IsNullOrEmpty(trackingNo))
                return ShipmentResult.Failed("HepsiJet tracking number alinamadi");

            _logger.LogInformation("HepsiJet shipment created: {TrackingNo}", trackingNo);
            return ShipmentResult.Succeeded(trackingNo, shipmentId ?? trackingNo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "HepsiJet CreateShipment failed for order {OrderId}", request.OrderId);
            return ShipmentResult.Failed($"HepsiJet hatasi: {ex.Message}");
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
                () => new HttpRequestMessage(HttpMethod.Get,
                    $"/api/v1/shipments/{trackingNumber}/tracking"), ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("HepsiJet tracking failed {Status} {Error}", response.StatusCode, errorBody);
                return trackingResult;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("status", out var statusProp))
                trackingResult.Status = MapHepsiJetStatus(statusProp.GetString() ?? "");

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
                            ? MapHepsiJetStatus(st.GetString() ?? "") : CargoStatus.Created
                    });
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "HepsiJet tracking failed for {TrackingNumber}", trackingNumber);
            trackingResult.Status = CargoStatus.Created;
        }

        return trackingResult;
    }

    // -- CancelShipmentAsync ---------------------------------
    public Task<bool> CancelShipmentAsync(string shipmentId, CancellationToken ct = default)
    {
        _logger.LogWarning("HepsiJet does not support shipment cancellation. ShipmentId: {ShipmentId}", shipmentId);
        return Task.FromResult(false);
    }

    // -- GetShipmentLabelAsync -------------------------------
    public async Task<LabelResult> GetShipmentLabelAsync(string shipmentId, CancellationToken ct = default)
    {
        EnsureConfigured();

        using var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get,
                $"/api/v1/shipments/{shipmentId}/label"), ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new HttpRequestException($"HepsiJet label request failed: {response.StatusCode} — {errorBody}");
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(content);
        var base64 = doc.RootElement.TryGetProperty("labelData", out var ld)
            ? ld.GetString()
            : doc.RootElement.TryGetProperty("pdfData", out var pd)
                ? pd.GetString() : null;

        if (string.IsNullOrEmpty(base64))
            throw new InvalidOperationException("HepsiJet etiket verisi bos");

        return new LabelResult
        {
            Data = Convert.FromBase64String(base64),
            Format = LabelFormat.Pdf,
            FileName = $"hepsijet-label-{shipmentId}.pdf"
        };
    }

    // -- Helpers ---------------------------------------------
    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "MesTech-HepsiJet-Client/1.0");
        return request;
    }

    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<HttpRequestMessage> requestFactory, CancellationToken ct)
    {
        await EnsureTokenAsync(ct).ConfigureAwait(false);
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _retryPipeline.ExecuteAsync(async token =>
            {
                using var request = requestFactory();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                request.Headers.TryAddWithoutValidation("User-Agent", "MesTech-HepsiJet-Client/1.0");
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "HepsiJet circuit breaker is open — returning 503");
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

    private static CargoStatus MapHepsiJetStatus(string status) => status.ToLowerInvariant() switch
    {
        "created" or "olusturuldu" => CargoStatus.Created,
        "pickedup" or "picked_up" or "alindi" => CargoStatus.PickedUp,
        "intransit" or "in_transit" or "transferde" => CargoStatus.InTransit,
        "outfordelivery" or "out_for_delivery" or "dagitimda" => CargoStatus.OutForDelivery,
        "delivered" or "teslim_edildi" => CargoStatus.Delivered,
        "returned" or "iade_edildi" => CargoStatus.Returned,
        "cancelled" or "iptal_edildi" => CargoStatus.Cancelled,
        "atbranch" or "at_branch" or "subede" => CargoStatus.AtBranch,
        "lost" or "kayip" => CargoStatus.Lost,
        _ => CargoStatus.Created
    };
}
