using System.Security;
using System.Xml.Linq;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Soap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Yurtici Kargo SOAP adaptoru.
/// SimpleSoapClient uzerinden XML/SOAP istekleri gonderir.
/// </summary>
public class YurticiKargoAdapter : ICargoAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YurticiKargoAdapter> _logger;
    private readonly YurticiKargoOptions _options;
    private readonly SimpleSoapClient _soapClient;

    private string _serviceUrl = string.Empty;
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private string _userLanguage = "TR";
    private bool _isConfigured;

    private static readonly XNamespace YkNs = "http://yurticikargo.com/";
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(5, 5);

    public YurticiKargoAdapter(HttpClient httpClient, ILogger<YurticiKargoAdapter> logger,
        IOptions<YurticiKargoOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new YurticiKargoOptions();
        _soapClient = new SimpleSoapClient(httpClient, logger);

        // Initialise service URL from options so sandbox toggle works before Configure() is called
        _serviceUrl = _options.ServiceUrl;
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

            await _soapClient.SendAsync(_serviceUrl, "http://yurticikargo.com/queryShipment", body, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── CreateShipmentAsync ─────────────────────────────
    public async Task<ShipmentResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken ct = default)
    {
        EnsureConfigured();
        ArgumentNullException.ThrowIfNull(request);

        await _rateLimitSemaphore.WaitAsync(ct);
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

            var result = await _soapClient.SendAsync(
                _serviceUrl, "http://yurticikargo.com/createShipment", body, ct);

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
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    // ── TrackShipmentAsync ──────────────────────────────
    public async Task<TrackingResult> TrackShipmentAsync(string trackingNumber, CancellationToken ct = default)
    {
        EnsureConfigured();

        var trackingResult = new TrackingResult { TrackingNumber = trackingNumber };

        await _rateLimitSemaphore.WaitAsync(ct);
        try
        {
            var body = new XElement(YkNs + "queryShipment",
                AuthElement(),
                new XElement(YkNs + "keys", SecurityElement.Escape(trackingNumber)));

            var result = await _soapClient.SendAsync(
                _serviceUrl, "http://yurticikargo.com/queryShipment", body, ct);

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
        finally
        {
            _rateLimitSemaphore.Release();
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

        await _rateLimitSemaphore.WaitAsync(ct);
        try
        {
            var body = new XElement(YkNs + "createShipmentLabel",
            AuthElement(),
            new XElement(YkNs + "keys", SecurityElement.Escape(shipmentId)));

        var result = await _soapClient.SendAsync(
            _serviceUrl, "http://yurticikargo.com/createShipmentLabel", body, ct);

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
        finally
        {
            _rateLimitSemaphore.Release();
        }
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
