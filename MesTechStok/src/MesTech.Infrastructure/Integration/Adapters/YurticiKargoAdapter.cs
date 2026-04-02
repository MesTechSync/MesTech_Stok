using System.Security;
using System.Xml.Linq;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Cargo;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Cargo;
using MesTech.Infrastructure.Integration.Soap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Diagnostics;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Yurtici Kargo SOAP adaptoru.
/// SimpleSoapClient uzerinden XML/SOAP istekleri gonderir.
/// </summary>
public sealed class YurticiKargoAdapter : ICargoAdapter, ICargoRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YurticiKargoAdapter> _logger;
    private readonly YurticiKargoOptions _options;
    private readonly SimpleSoapClient _soapClient;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(5, 5);

    private string _serviceUrl = string.Empty;
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private string _userLanguage = "TR";
    private bool _isConfigured;

    private static readonly XNamespace YkNs = "http://yurticikargo.com/";

    public YurticiKargoAdapter(HttpClient httpClient, ILogger<YurticiKargoAdapter> logger,
        IOptions<YurticiKargoOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new YurticiKargoOptions();
        _soapClient = new SimpleSoapClient(httpClient, logger);

        // Initialise service URL from options so sandbox toggle works before Configure() is called
        _serviceUrl = _options.ServiceUrl;

        // SSRF guard (G10853)
        if (Uri.TryCreate(_serviceUrl, UriKind.Absolute, out var uri) && Security.SsrfGuard.IsPrivateHost(uri.Host))
            _logger.LogWarning("[YurticiKargoAdapter] ServiceUrl points to private network: {Url}", _serviceUrl);

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
                    _logger.LogWarning("[YurticiKargo] Rate limited (429). Retry {Attempt} after {Delay}ms",
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
                        "[YurticiKargoAdapter] API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[YurticiKargoAdapter] Circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public CargoProvider Provider => CargoProvider.YurticiKargo;
    public bool SupportsCancellation => false;
    public bool SupportsLabelGeneration => true;
    public bool SupportsCashOnDelivery => true;
    public bool SupportsMultiParcel => true;

    public void Configure(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _userName = credentials.GetValueOrDefault("UserName", "");
        _password = credentials.GetValueOrDefault("Password", "");
        _userLanguage = credentials.GetValueOrDefault("UserLanguage", "TR");

        // Support ServiceUrl override from credentials (backwards compatible)
        var credServiceUrl = credentials.GetValueOrDefault("ServiceUrl", "");
        if (!string.IsNullOrEmpty(credServiceUrl))
            _serviceUrl = credServiceUrl;

        // UseSandbox=true shortcut sets sandbox URL automatically
        if (credentials.GetValueOrDefault("UseSandbox", "false").Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            _serviceUrl = _options.SandboxServiceUrl;
        }

        // Fallback: if no URL was set via credentials or sandbox toggle, use options
        if (string.IsNullOrEmpty(_serviceUrl))
            _serviceUrl = _options.ServiceUrl;

        _isConfigured = true;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException("YurticiKargoAdapter henuz konfigure edilmedi.");
    }

    private async Task<XElement> ThrottledSoapAsync(
        string url, string soapAction, XElement body, CancellationToken ct)
    {
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _soapClient.SendAsync(url, soapAction, body, ct).ConfigureAwait(false);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    // ── ICargoAdapter.IsAvailableAsync ──────────────────
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_isConfigured) return false;
        try
        {
            // Basit ping: bos bir query dene
            var body = new XElement(YkNs + "queryShipment",
                AuthElement(),
                new XElement(YkNs + "keys", "PING-TEST"));

            await ThrottledSoapAsync(_serviceUrl, "https://yurticikargo.com/queryShipment", body, ct).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[YurticiKargoAdapter] IsAvailableAsync health check failed");
            return false;
        }
    }

    // ── CreateShipmentAsync ─────────────────────────────
    public async Task<ShipmentResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var body = new XElement(YkNs + "createShipment",
                AuthElement(),
                new XElement(YkNs + "ShipmentInfoVO",
                    El("cargoKey", request.OrderId.ToString()),
                    El("invoiceKey", request.OrderId.ToString()),
                    El("receiverCustName", SecurityElement.Escape(request.RecipientName)),
                    El("receiverPhone", SecurityElement.Escape(request.RecipientPhone)),
                    El("receiverAddress", SecurityElement.Escape(request.RecipientAddress.FullAddress)),
                    El("receiverCity", SecurityElement.Escape(request.RecipientAddress.City)),
                    El("receiverTown", SecurityElement.Escape(request.RecipientAddress.District)),
                    El("desi", request.Desi.ToString()),
                    El("kg", request.Weight.ToString("F2")),
                    El("cargoCount", request.ParcelCount.ToString()),
                    El("specialField1", request.Notes ?? ""),
                    El("ttDocumentId", ""),
                    El("dcSelectedCredit", ""),
                    El("dcCreditRule", request.CodAmount.HasValue ? "1" : "0")));

            var result = await ThrottledSoapAsync(
                _serviceUrl, "http://yurticikargo.com/createShipment", body, ct).ConfigureAwait(false);

            SimpleSoapClient.ThrowIfFault(result);

            var trackingNo = result.Descendants("cargoKey").FirstOrDefault()?.Value
                          ?? result.Descendants("invDocId").FirstOrDefault()?.Value;
            var shipmentId = result.Descendants("jobId").FirstOrDefault()?.Value ?? trackingNo;

            if (string.IsNullOrEmpty(trackingNo))
                return ShipmentResult.Failed("Yurtici Kargo tracking number alinamadi");

            _logger.LogInformation("Yurtici Kargo shipment created: {TrackingNo}", trackingNo);
            return ShipmentResult.Succeeded(trackingNo, shipmentId ?? trackingNo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Yurtici Kargo CreateShipment failed for order {OrderId}", request.OrderId);
            return ShipmentResult.Failed($"Yurtici Kargo hatasi: {ex.Message}");
        }
    }

    // ── TrackShipmentAsync ──────────────────────────────
    public async Task<TrackingResult> TrackShipmentAsync(string trackingNumber, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        EnsureConfigured();

        var trackingResult = new TrackingResult { TrackingNumber = trackingNumber };

        try
        {
            var body = new XElement(YkNs + "queryShipment",
                AuthElement(),
                new XElement(YkNs + "keys", SecurityElement.Escape(trackingNumber)));

            var result = await ThrottledSoapAsync(
                _serviceUrl, "http://yurticikargo.com/queryShipment", body, ct).ConfigureAwait(false);

            SimpleSoapClient.ThrowIfFault(result);

            var statusCode = result.Descendants("operationCode").FirstOrDefault()?.Value ?? "";
            trackingResult.Status = MapYkStatus(statusCode);

            foreach (var movement in result.Descendants("ShipmentEventVO"))
            {
                trackingResult.Events.Add(new TrackingEvent
                {
                    Timestamp = DateTime.TryParse(movement.Element("eventDate")?.Value, out var dt)
                        ? dt : DateTime.UtcNow,
                    Location = movement.Element("unitName")?.Value ?? "",
                    Description = movement.Element("eventName")?.Value ?? "",
                    Status = MapYkStatus(movement.Element("operationCode")?.Value ?? "")
                });
            }

            var estimatedDate = result.Descendants("estimatedArrivalDate").FirstOrDefault()?.Value;
            if (DateTime.TryParse(estimatedDate, out var eta))
                trackingResult.EstimatedDelivery = eta;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Yurtici Kargo tracking failed for {TrackingNumber}", trackingNumber);
            trackingResult.Status = CargoStatus.Created;
        }

        return trackingResult;
    }

    // ── CancelShipmentAsync ─────────────────────────────
    public Task<bool> CancelShipmentAsync(string shipmentId, CancellationToken ct = default)
    {
        _logger.LogWarning("Yurtici Kargo does not support cancellation via API");
        return Task.FromResult(false);
    }

    // ── GetShipmentLabelAsync ───────────────────────────
    public async Task<LabelResult> GetShipmentLabelAsync(string shipmentId, CancellationToken ct = default)
    {
        EnsureConfigured();

        var body = new XElement(YkNs + "createShipmentLabel",
            AuthElement(),
            new XElement(YkNs + "keys", SecurityElement.Escape(shipmentId)));

        var result = await ThrottledSoapAsync(
            _serviceUrl, "http://yurticikargo.com/createShipmentLabel", body, ct).ConfigureAwait(false);

        SimpleSoapClient.ThrowIfFault(result);

        var base64Data = result.Descendants("labelData").FirstOrDefault()?.Value;
        if (string.IsNullOrEmpty(base64Data))
            throw new InvalidOperationException("Yurtici Kargo etiket verisi bos");

        return new LabelResult
        {
            Data = Convert.FromBase64String(base64Data),
            Format = LabelFormat.Pdf,
            FileName = $"yk-label-{shipmentId}.pdf"
        };
    }

    // ── ICargoRateProvider ─────────────────────────────
    public Task<CargoRateResult?> GetRateAsync(ShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var rate = DesiBasedCargoRateCalculator.Calculate(Provider, request);
        return Task.FromResult<CargoRateResult?>(rate);
    }

    // ── Helpers ─────────────────────────────────────────
    private XElement AuthElement() => new(YkNs + "wsUserInfo",
        El("wsUserName", SecurityElement.Escape(_userName)),
        El("wsPassword", SecurityElement.Escape(_password)),
        El("userLanguage", SecurityElement.Escape(_userLanguage)));

    private static XElement El(string name, string value) => new(name, value);

    private static CargoStatus MapYkStatus(string code) => code switch
    {
        "1" => CargoStatus.Created,
        "2" => CargoStatus.PickedUp,
        "3" => CargoStatus.InTransit,
        "4" => CargoStatus.OutForDelivery,
        "5" => CargoStatus.Delivered,
        "6" => CargoStatus.Returned,
        _ => CargoStatus.Created
    };
}
