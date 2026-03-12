using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Surat Kargo REST adaptoru.
/// Basic Auth, JSON payloads, Polly retry.
/// </summary>
public class SuratKargoAdapter : ICargoAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SuratKargoAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10);

    private string _customerCode = string.Empty;
    private bool _isConfigured;

    public SuratKargoAdapter(HttpClient httpClient, ILogger<SuratKargoAdapter> logger)
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
                    _logger.LogWarning("Surat Kargo API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("SuratKargo circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public CargoProvider Provider => CargoProvider.SuratKargo;
    public bool SupportsCancellation => true;
    public bool SupportsLabelGeneration => true;
    public bool SupportsCashOnDelivery => false;
    public bool SupportsMultiParcel => false;

    public void Configure(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var userName = credentials.GetValueOrDefault("UserName", "");
        var password = credentials.GetValueOrDefault("Password", "");
        _customerCode = credentials.GetValueOrDefault("CustomerCode", "");

        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MesTech-SuratKargo-Client/3.0");

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _httpClient.BaseAddress = new Uri(credentials["BaseUrl"], UriKind.Absolute);

        _isConfigured = true;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException("SuratKargoAdapter henuz konfigure edilmedi.");
    }

    // -- ICargoAdapter.IsAvailableAsync ----------------------
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_isConfigured) return false;
        try
        {
            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get, "/api/v2/health"), ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
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
            var response = await ExecuteWithRetryAsync(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/cargo/create");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return req;
            }, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("Surat Kargo CreateShipment failed {Status}: {Error}",
                    response.StatusCode, error);
                return ShipmentResult.Failed($"Surat Kargo hatasi: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);
            var trackingNo = doc.RootElement.TryGetProperty("trackingNumber", out var tn)
                ? tn.GetString() : null;
            var shipmentId = doc.RootElement.TryGetProperty("shipmentId", out var si)
                ? si.GetString() : null;

            if (string.IsNullOrEmpty(trackingNo))
                return ShipmentResult.Failed("Surat Kargo tracking number alinamadi");

            _logger.LogInformation("Surat Kargo shipment created: {TrackingNo}", trackingNo);
            return ShipmentResult.Succeeded(trackingNo, shipmentId ?? trackingNo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Surat Kargo CreateShipment failed for order {OrderId}", request.OrderId);
            return ShipmentResult.Failed($"Surat Kargo hatasi: {ex.Message}");
        }
    }

    // -- TrackShipmentAsync ----------------------------------
    public async Task<TrackingResult> TrackShipmentAsync(string trackingNumber, CancellationToken ct = default)
    {
        EnsureConfigured();
        var trackingResult = new TrackingResult { TrackingNumber = trackingNumber };

        try
        {
            var response = await ExecuteWithRetryAsync(
                () => new HttpRequestMessage(HttpMethod.Get,
                    $"/api/v2/cargo/{trackingNumber}/status"), ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Surat Kargo tracking failed {Status}", response.StatusCode);
                return trackingResult;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("status", out var statusProp))
                trackingResult.Status = MapSuratStatus(statusProp.GetString() ?? "");

            if (doc.RootElement.TryGetProperty("estimatedDelivery", out var eta) &&
                DateTime.TryParse(eta.GetString(), out var etaDate))
                trackingResult.EstimatedDelivery = etaDate;

            if (doc.RootElement.TryGetProperty("events", out var events))
            {
                foreach (var evt in events.EnumerateArray())
                {
                    trackingResult.Events.Add(new TrackingEvent
                    {
                        Timestamp = DateTime.TryParse(
                            evt.GetProperty("timestamp").GetString(), out var ts)
                            ? ts : DateTime.UtcNow,
                        Location = evt.TryGetProperty("location", out var loc)
                            ? loc.GetString() ?? "" : "",
                        Description = evt.TryGetProperty("description", out var desc)
                            ? desc.GetString() ?? "" : "",
                        Status = evt.TryGetProperty("status", out var st)
                            ? MapSuratStatus(st.GetString() ?? "") : CargoStatus.Created
                    });
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Surat Kargo tracking failed for {TrackingNumber}", trackingNumber);
            trackingResult.Status = CargoStatus.Created;
        }

        return trackingResult;
    }

    // -- CancelShipmentAsync ---------------------------------
    public async Task<bool> CancelShipmentAsync(string shipmentId, CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Delete,
                $"/api/v2/cargo/{shipmentId}"), ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Surat Kargo shipment cancelled: {ShipmentId}", shipmentId);
            return true;
        }

        _logger.LogWarning("Surat Kargo cancel failed {Status}", response.StatusCode);
        return false;
    }

    // -- GetShipmentLabelAsync -------------------------------
    public async Task<LabelResult> GetShipmentLabelAsync(string shipmentId, CancellationToken ct = default)
    {
        EnsureConfigured();

        var response = await ExecuteWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get,
                $"/api/v2/cargo/{shipmentId}/label"), ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Surat Kargo label request failed: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(content);
        var base64 = doc.RootElement.GetProperty("labelData").GetString();

        if (string.IsNullOrEmpty(base64))
            throw new InvalidOperationException("Surat Kargo etiket verisi bos");

        return new LabelResult
        {
            Data = Convert.FromBase64String(base64),
            Format = LabelFormat.Pdf,
            FileName = $"surat-label-{shipmentId}.pdf"
        };
    }

    // -- Helpers ---------------------------------------------
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
            _logger.LogWarning(ex, "SuratKargo circuit breaker is open — returning 503");
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

    private static CargoStatus MapSuratStatus(string status) => status.ToLowerInvariant() switch
    {
        "created" => CargoStatus.Created,
        "pickedup" or "picked_up" => CargoStatus.PickedUp,
        "intransit" or "in_transit" => CargoStatus.InTransit,
        "outfordelivery" or "out_for_delivery" => CargoStatus.OutForDelivery,
        "delivered" => CargoStatus.Delivered,
        "returned" => CargoStatus.Returned,
        "cancelled" => CargoStatus.Cancelled,
        "atbranch" or "at_branch" => CargoStatus.AtBranch,
        "lost" => CargoStatus.Lost,
        _ => CargoStatus.Created
    };
}
