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
/// DHL Express REST API adaptoru (MyDHL API).
/// Auth: Basic Auth (Base64 clientId:secretKey).
/// Base URL: https://express.api.dhl.com/mydhlapi (production)
///           https://express.api.dhl.com/mydhlapi/test (sandbox)
/// Endpoints: POST /shipments, GET /track/shipments, GET /shipments/{id}/label, DELETE /shipments/{id}
/// </summary>
public sealed class DhlExpressAdapter : ICargoAdapter, ICargoRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DhlExpressAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);

    private string _accountNumber = string.Empty;
    private string _basicAuthValue = string.Empty;
    private bool _isConfigured;

    // DHL Express tracking uses a separate endpoint
    private const string TrackingBaseUrl = "https://api-eu.dhl.com/track/shipments";
    private string _trackingApiKey = string.Empty;

    public DhlExpressAdapter(HttpClient httpClient, ILogger<DhlExpressAdapter> logger)
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
                    _logger.LogWarning("[DHL] Retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[DHL] Circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public CargoProvider Provider => CargoProvider.DHL;
    public bool SupportsCancellation => true;
    public bool SupportsLabelGeneration => true;
    public bool SupportsCashOnDelivery => false; // DHL Express COD limited
    public bool SupportsMultiParcel => true;

    public void Configure(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var clientId = credentials.GetValueOrDefault("ClientId", "");
        var secretKey = credentials.GetValueOrDefault("SecretKey", "");
        _accountNumber = credentials.GetValueOrDefault("AccountNumber", "");
        _trackingApiKey = credentials.GetValueOrDefault("TrackingApiKey", "");

        _basicAuthValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{secretKey}"));

        var useSandbox = credentials.GetValueOrDefault("UseSandbox", "false")
            .Equals("true", StringComparison.OrdinalIgnoreCase);
        var baseUrl = useSandbox
            ? "https://express.api.dhl.com/mydhlapi/test"
            : "https://express.api.dhl.com/mydhlapi";

        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsedUri))
        {
            if (SsrfGuard.IsPrivateHost(parsedUri.Host))
                _logger.LogWarning("[DHL] BaseUrl points to private network: {BaseUrl}", baseUrl);
            _httpClient.BaseAddress = parsedUri;
        }

        _isConfigured = true;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException("DhlExpressAdapter henuz konfigure edilmedi.");
    }

    // -- ICargoAdapter.IsAvailableAsync ----------------------
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_isConfigured) return false;
        try
        {
            using var response = await ExecuteWithRetryAsync(
                () => CreateAuthenticatedRequest(HttpMethod.Get, "/rates"), ct).ConfigureAwait(false);
            return (int)response.StatusCode < 500;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "[DHL] IsAvailableAsync health check failed");
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
            var payload = new
            {
                productCode = "P", // DHL Express Worldwide
                accounts = new[] { new { typeCode = "shipper", number = _accountNumber } },
                customerDetails = new
                {
                    shipperDetails = new
                    {
                        postalAddress = new
                        {
                            cityName = request.SenderAddress.City,
                            countryCode = "TR",
                            postalCode = request.SenderAddress.PostalCode,
                            addressLine1 = request.SenderAddress.FullAddress
                        },
                        contactInformation = new { phone = "0000000000", companyName = "MesTech", fullName = "MesTech Shipper" }
                    },
                    receiverDetails = new
                    {
                        postalAddress = new
                        {
                            cityName = request.RecipientAddress.City,
                            countryCode = "TR",
                            postalCode = request.RecipientAddress.PostalCode,
                            addressLine1 = request.RecipientAddress.FullAddress
                        },
                        contactInformation = new
                        {
                            phone = request.RecipientPhone,
                            fullName = request.RecipientName
                        }
                    }
                },
                content = new
                {
                    packages = new[]
                    {
                        new
                        {
                            weight = (float)request.Weight,
                            dimensions = new { length = 30, width = 20, height = 15 }
                        }
                    },
                    declaredValue = 0,
                    unitOfMeasurement = "metric",
                    description = request.Notes ?? "E-commerce shipment"
                },
                outputImageProperties = new
                {
                    encodingFormat = "pdf",
                    imageOptions = new[] { new { typeCode = "label" } }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var response = await ExecuteWithRetryAsync(() =>
            {
                var req = CreateAuthenticatedRequest(HttpMethod.Post, "/shipments");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("[DHL] CreateShipment failed {Status}: {Error}", response.StatusCode, error);
                return ShipmentResult.Failed($"DHL hatasi: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var trackingNo = doc.RootElement.TryGetProperty("shipmentTrackingNumber", out var tn)
                ? tn.GetString() : null;
            var shipmentId = doc.RootElement.TryGetProperty("dispatchConfirmationNumber", out var dc)
                ? dc.GetString() : trackingNo;

            if (string.IsNullOrEmpty(trackingNo))
                return ShipmentResult.Failed("DHL tracking number alinamadi");

            _logger.LogInformation("[DHL] Shipment created: {TrackingNo}", trackingNo);
            return ShipmentResult.Succeeded(trackingNo, shipmentId ?? trackingNo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[DHL] CreateShipment failed for order {OrderId}", request.OrderId);
            return ShipmentResult.Failed($"DHL hatasi: {ex.Message}");
        }
    }

    // -- TrackShipmentAsync ----------------------------------
    public async Task<TrackingResult> TrackShipmentAsync(string trackingNumber, CancellationToken ct = default)
    {
        EnsureConfigured();
        var trackingResult = new TrackingResult { TrackingNumber = trackingNumber };

        try
        {
            // DHL tracking uses separate endpoint + API key auth
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{TrackingBaseUrl}?trackingNumber={trackingNumber}&service=express&language=tr");
            if (!string.IsNullOrEmpty(_trackingApiKey))
                request.Headers.TryAddWithoutValidation("DHL-API-Key", _trackingApiKey);

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
                    _logger.LogWarning("[DHL] Tracking failed {Status}", response.StatusCode);
                    return trackingResult;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("shipments", out var shipments)
                    && shipments.ValueKind == JsonValueKind.Array)
                {
                    foreach (var shipment in shipments.EnumerateArray())
                    {
                        if (shipment.TryGetProperty("status", out var statusProp)
                            && statusProp.TryGetProperty("statusCode", out var code))
                            trackingResult.Status = MapDhlStatus(code.GetString() ?? "");

                        if (shipment.TryGetProperty("events", out var events))
                        {
                            foreach (var evt in events.EnumerateArray())
                            {
                                trackingResult.Events.Add(new TrackingEvent
                                {
                                    Timestamp = evt.TryGetProperty("timestamp", out var ts)
                                        && DateTime.TryParse(ts.GetString(), out var dt)
                                        ? dt : DateTime.UtcNow,
                                    Location = evt.TryGetProperty("location", out var loc)
                                        && loc.TryGetProperty("address", out var addr)
                                        && addr.TryGetProperty("addressLocality", out var city)
                                        ? city.GetString() ?? "" : "",
                                    Description = evt.TryGetProperty("description", out var desc)
                                        ? desc.GetString() ?? "" : "",
                                    Status = evt.TryGetProperty("statusCode", out var sc)
                                        ? MapDhlStatus(sc.GetString() ?? "") : CargoStatus.Created
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[DHL] Tracking failed for {TrackingNumber}", trackingNumber);
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
            using var response = await ExecuteWithRetryAsync(
                () => CreateAuthenticatedRequest(HttpMethod.Delete, $"/shipments/{shipmentId}"), ct)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogInformation("[DHL] Shipment cancelled: {ShipmentId}", shipmentId);
                return true;
            }

            _logger.LogWarning("[DHL] Cancel failed {Status}", response.StatusCode);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[DHL] CancelShipment failed: {ShipmentId}", shipmentId);
            return false;
        }
    }

    // -- GetShipmentLabelAsync -------------------------------
    public async Task<LabelResult> GetShipmentLabelAsync(string shipmentId, CancellationToken ct = default)
    {
        EnsureConfigured();

        using var response = await ExecuteWithRetryAsync(
            () => CreateAuthenticatedRequest(HttpMethod.Get,
                $"/shipments/{shipmentId}/label?labelFormat=PDF"), ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new HttpRequestException($"DHL label request failed: {response.StatusCode} — {errorBody}");
        }

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(content);

        var base64 = doc.RootElement.TryGetProperty("labelData", out var ld)
            ? ld.GetString()
            : doc.RootElement.TryGetProperty("documents", out var docs)
                && docs.ValueKind == JsonValueKind.Array
                ? docs.EnumerateArray().FirstOrDefault().TryGetProperty("content", out var c)
                    ? c.GetString() : null
                : null;

        if (string.IsNullOrEmpty(base64))
            throw new InvalidOperationException("DHL etiket verisi bos");

        return new LabelResult
        {
            Data = Convert.FromBase64String(base64),
            Format = LabelFormat.Pdf,
            FileName = $"dhl-label-{shipmentId}.pdf"
        };
    }

    // -- Helpers ---------------------------------------------
    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(_basicAuthValue))
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _basicAuthValue);
        request.Headers.TryAddWithoutValidation("User-Agent", "MesTech-DHL-Client/1.0");
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
            _logger.LogWarning(ex, "[DHL] Circuit breaker open — returning 503");
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

    // -- ICargoRateProvider ----------------------------------
    public Task<CargoRateResult?> GetRateAsync(ShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var rate = DesiBasedCargoRateCalculator.Calculate(Provider, request);
        return Task.FromResult<CargoRateResult?>(rate);
    }

    private static CargoStatus MapDhlStatus(string status) => status.ToUpperInvariant() switch
    {
        "PRE-TRANSIT" or "BOOKING" => CargoStatus.Created,
        "TRANSIT" or "IN_TRANSIT" => CargoStatus.InTransit,
        "OUT_FOR_DELIVERY" or "DELIVERY" => CargoStatus.OutForDelivery,
        "DELIVERED" => CargoStatus.Delivered,
        "RETURNED" or "RETURN" => CargoStatus.Returned,
        "FAILURE" or "EXCEPTION" => CargoStatus.Lost,
        _ => CargoStatus.Created
    };
}
