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
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// UPS REST API adaptoru.
/// Auth: OAuth 2.0 Client Credentials (POST /security/v1/oauth/token).
/// Base URL: https://onlinetools.ups.com/api (production)
///           https://wwwcie.ups.com/api (sandbox/CIE)
/// Endpoints: POST /shipments/v1/ship, GET /track/v1/details/{id},
///            DELETE /shipments/v1/void/cancel/{id}
/// </summary>
public sealed class UpsAdapter : ICargoAdapter, ICargoRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpsAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);
    private static readonly SemaphoreSlim _tokenRefreshLock = new(1, 1);

    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;
    private string _accountNumber = string.Empty;
    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private string _baseUrl = string.Empty;
    private string _tokenEndpoint = string.Empty;
    private bool _isConfigured;

    private static readonly TimeSpan TokenBuffer = TimeSpan.FromMinutes(5);

    public UpsAdapter(HttpClient httpClient, ILogger<UpsAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
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
                DelayGenerator = args =>
                {
                    if (args.Outcome.Result is { StatusCode: System.Net.HttpStatusCode.TooManyRequests } resp
                        && resp.Headers.RetryAfter is { } ra)
                        return new ValueTask<TimeSpan?>(ra.Delta ?? TimeSpan.FromSeconds(5));
                    return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber)));
                },
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    .HandleResult(r => (int)r.StatusCode >= 500)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning("[UPS] Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[UPS] Circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public CargoProvider Provider => CargoProvider.UPS;
    public bool SupportsCancellation => true;
    public bool SupportsLabelGeneration => true;
    public bool SupportsCashOnDelivery => false;
    public bool SupportsMultiParcel => true;

    public void Configure(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        _clientId = credentials.GetValueOrDefault("ClientId", "");
        _clientSecret = credentials.GetValueOrDefault("ClientSecret", "");
        _accountNumber = credentials.GetValueOrDefault("AccountNumber", "");

        var useSandbox = credentials.GetValueOrDefault("UseSandbox", "false")
            .Equals("true", StringComparison.OrdinalIgnoreCase);
        _baseUrl = useSandbox
            ? "https://wwwcie.ups.com/api"
            : "https://onlinetools.ups.com/api";
        _tokenEndpoint = useSandbox
            ? "https://wwwcie.ups.com/security/v1/oauth/token"
            : "https://onlinetools.ups.com/security/v1/oauth/token";

        if (Uri.TryCreate(_baseUrl, UriKind.Absolute, out var parsedUri) && SsrfGuard.IsPrivateHost(parsedUri.Host))
            _logger.LogWarning("[UPS] BaseUrl points to private network: {BaseUrl}", _baseUrl);

        _isConfigured = true;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException("UpsAdapter henuz konfigure edilmedi.");
    }

    // -- OAuth Token -----------------------------------------
    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry - TokenBuffer)
            return _accessToken;

        await _tokenRefreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry - TokenBuffer)
                return _accessToken;

            var basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            });

            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var json = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
                cancellationToken: ct).ConfigureAwait(false);

            _accessToken = json.RootElement.TryGetProperty("access_token", out var atProp)
                ? atProp.GetString() ?? string.Empty : string.Empty;
            var expiresIn = json.RootElement.TryGetProperty("expires_in", out var eiProp)
                ? (eiProp.ValueKind == JsonValueKind.Number ? eiProp.GetInt32()
                    : int.TryParse(eiProp.GetString(), out var parsed) ? parsed : 3600)
                : 3600;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

            _logger.LogInformation("[UPS] OAuth2 token refreshed — expires in {Seconds}s", expiresIn);
            return _accessToken;
        }
        finally
        {
            _tokenRefreshLock.Release();
        }
    }

    // -- ICargoAdapter.IsAvailableAsync ----------------------
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_isConfigured) return false;
        try
        {
            await GetAccessTokenAsync(ct).ConfigureAwait(false);
            return true; // Token alınabildiyse API erişilebilir
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "[UPS] IsAvailableAsync health check failed");
            return false;
        }
    }

    // -- CreateShipmentAsync ---------------------------------
    public async Task<ShipmentResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken ct = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                ShipmentRequest = new
                {
                    Request = new { RequestOption = "nonvalidate" },
                    Shipment = new
                    {
                        Description = request.Notes ?? "E-commerce shipment",
                        Shipper = new
                        {
                            Name = "MesTech",
                            ShipperNumber = _accountNumber,
                            Address = new
                            {
                                AddressLine = new[] { request.SenderAddress.FullAddress },
                                City = request.SenderAddress.City,
                                PostalCode = request.SenderAddress.PostalCode,
                                CountryCode = "TR"
                            },
                            Phone = new { Number = "0000000000" }
                        },
                        ShipTo = new
                        {
                            Name = request.RecipientName,
                            Address = new
                            {
                                AddressLine = new[] { request.RecipientAddress.FullAddress },
                                City = request.RecipientAddress.City,
                                PostalCode = request.RecipientAddress.PostalCode,
                                CountryCode = "TR"
                            },
                            Phone = new { Number = request.RecipientPhone }
                        },
                        PaymentInformation = new
                        {
                            ShipmentCharge = new[]
                            {
                                new { Type = "01", BillShipper = new { AccountNumber = _accountNumber } }
                            }
                        },
                        Service = new { Code = "65", Description = "UPS Saver" }, // UPS Saver for Turkey
                        Package = new[]
                        {
                            new
                            {
                                PackagingType = new { Code = "02" }, // Customer Supplied Package
                                PackageWeight = new
                                {
                                    UnitOfMeasurement = new { Code = "KGS" },
                                    Weight = request.Weight.ToString("F1")
                                }
                            }
                        }
                    },
                    LabelSpecification = new
                    {
                        LabelImageFormat = new { Code = "PDF" },
                        LabelStockSize = new { Height = "6", Width = "4" }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/shipments/v1/ship");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Headers.TryAddWithoutValidation("transId", Guid.NewGuid().ToString("N"));
            httpRequest.Headers.TryAddWithoutValidation("transactionSrc", "MesTech");
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
            HttpResponseMessage response;
            try
            {
                response = await _retryPipeline.ExecuteAsync(async _ =>
                    await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false), ct).ConfigureAwait(false);
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("[UPS] CreateShipment failed {Status}: {Error}", response.StatusCode, error);
                    return ShipmentResult.Failed($"UPS hatasi: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                var trackingNo = doc.RootElement
                    .TryGetProperty("ShipmentResponse", out var sr)
                    && sr.TryGetProperty("ShipmentResults", out var results)
                    && results.TryGetProperty("PackageResults", out var pkgResults)
                    && pkgResults.ValueKind == JsonValueKind.Array
                    ? pkgResults.EnumerateArray().FirstOrDefault()
                        .TryGetProperty("TrackingNumber", out var tn) ? tn.GetString() : null
                    : null;

                if (string.IsNullOrEmpty(trackingNo))
                    return ShipmentResult.Failed("UPS tracking number alinamadi");

                _logger.LogInformation("[UPS] Shipment created: {TrackingNo}", trackingNo);
                return ShipmentResult.Succeeded(trackingNo, trackingNo);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[UPS] CreateShipment failed for order {OrderId}", request.OrderId);
            return ShipmentResult.Failed($"UPS hatasi: {ex.Message}");
        }
    }

    // -- TrackShipmentAsync ----------------------------------
    public async Task<TrackingResult> TrackShipmentAsync(string trackingNumber, CancellationToken ct = default)
    {
        EnsureConfigured();
        var trackingResult = new TrackingResult { TrackingNumber = trackingNumber };

        try
        {
            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);

            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_baseUrl}/track/v1/details/{trackingNumber}?locale=tr_TR");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.TryAddWithoutValidation("transId", Guid.NewGuid().ToString("N"));
            request.Headers.TryAddWithoutValidation("transactionSrc", "MesTech");

            await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[UPS] Tracking failed {Status}", response.StatusCode);
                    return trackingResult;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("trackResponse", out var tr)
                    && tr.TryGetProperty("shipment", out var shipments)
                    && shipments.ValueKind == JsonValueKind.Array)
                {
                    foreach (var shipment in shipments.EnumerateArray())
                    {
                        if (shipment.TryGetProperty("package", out var packages)
                            && packages.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var pkg in packages.EnumerateArray())
                            {
                                if (pkg.TryGetProperty("currentStatus", out var cs)
                                    && cs.TryGetProperty("code", out var statusCode))
                                    trackingResult.Status = MapUpsStatus(statusCode.GetString() ?? "");

                                if (pkg.TryGetProperty("activity", out var activities)
                                    && activities.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var act in activities.EnumerateArray())
                                    {
                                        trackingResult.Events.Add(new TrackingEvent
                                        {
                                            Timestamp = act.TryGetProperty("date", out var dateProp)
                                                && act.TryGetProperty("time", out var timeProp)
                                                && DateTime.TryParse($"{dateProp.GetString()} {timeProp.GetString()}", out var dt)
                                                ? dt : DateTime.UtcNow,
                                            Location = act.TryGetProperty("location", out var loc)
                                                && loc.TryGetProperty("address", out var addr)
                                                && addr.TryGetProperty("city", out var city)
                                                ? city.GetString() ?? "" : "",
                                            Description = act.TryGetProperty("status", out var st)
                                                && st.TryGetProperty("description", out var desc)
                                                ? desc.GetString() ?? "" : "",
                                            Status = act.TryGetProperty("status", out var st2)
                                                && st2.TryGetProperty("code", out var sc)
                                                ? MapUpsStatus(sc.GetString() ?? "") : CargoStatus.Created
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[UPS] Tracking failed for {TrackingNumber}", trackingNumber);
            trackingResult.Status = CargoStatus.Created;
        }

        return trackingResult;
    }

    // -- CancelShipmentAsync ---------------------------------
    public async Task<bool> CancelShipmentAsync(string shipmentId, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);

            using var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{_baseUrl}/shipments/v1/void/cancel/{shipmentId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.TryAddWithoutValidation("transId", Guid.NewGuid().ToString("N"));
            request.Headers.TryAddWithoutValidation("transactionSrc", "MesTech");

            await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }

            using (response)
            {
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation("[UPS] Shipment cancelled: {ShipmentId}", shipmentId);
                    return true;
                }

                _logger.LogWarning("[UPS] Cancel failed {Status}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[UPS] CancelShipment failed: {ShipmentId}", shipmentId);
            return false;
        }
    }

    // -- GetShipmentLabelAsync -------------------------------
    public async Task<LabelResult> GetShipmentLabelAsync(string shipmentId, CancellationToken ct = default)
    {
        // UPS returns label data in the shipment creation response.
        // For re-fetch, use the label recovery endpoint.
        EnsureConfigured();

        var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_baseUrl}/shipments/v1/labels/{shipmentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("transId", Guid.NewGuid().ToString("N"));
        request.Headers.TryAddWithoutValidation("transactionSrc", "MesTech");

        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new HttpRequestException($"UPS label request failed: {response.StatusCode} — {errorBody}");
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var base64 = doc.RootElement.TryGetProperty("labelData", out var ld)
                ? ld.GetString()
                : doc.RootElement.TryGetProperty("LabelRecoveryResponse", out var lrr)
                    && lrr.TryGetProperty("LabelResults", out var lr)
                    && lr.TryGetProperty("LabelImage", out var li)
                    && li.TryGetProperty("GraphicImage", out var gi)
                    ? gi.GetString() : null;

            if (string.IsNullOrEmpty(base64))
                throw new InvalidOperationException("UPS etiket verisi bos");

            return new LabelResult
            {
                Data = Convert.FromBase64String(base64),
                Format = LabelFormat.Pdf,
                FileName = $"ups-label-{shipmentId}.pdf"
            };
        }
    }

    // -- ICargoRateProvider ----------------------------------
    public Task<CargoRateResult?> GetRateAsync(ShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var rate = DesiBasedCargoRateCalculator.Calculate(Provider, request);
        return Task.FromResult<CargoRateResult?>(rate);
    }

    private static CargoStatus MapUpsStatus(string status) => status.ToUpperInvariant() switch
    {
        "M" or "MP" or "MV" => CargoStatus.Created,         // Manifest Pickup
        "I" or "IT" => CargoStatus.InTransit,                // In Transit
        "O" or "OT" => CargoStatus.OutForDelivery,           // Out for Delivery
        "D" or "DL" or "KB" => CargoStatus.Delivered,        // Delivered
        "RS" or "NA" => CargoStatus.Returned,                // Returned to Sender
        "X" or "DN" => CargoStatus.Cancelled,                // Exception / Denied
        _ => CargoStatus.Created
    };
}
