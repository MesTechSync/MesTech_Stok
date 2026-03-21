using System.Security;
using System.Xml.Linq;
using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Soap;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// PTT Kargo SOAP adaptoru.
/// SimpleSoapClient uzerinden XML/SOAP istekleri gonderir.
/// Shipment WSDL: https://pttws.ptt.gov.tr/PttVeriYukleme/services/Sorgu?wsdl
/// Tracking WSDL: https://pttws.ptt.gov.tr/GonderiTakip/services/Sorgu?wsdl
/// Auth: Username + Password + MusteriId (from credentials)
/// </summary>
public class PttKargoAdapter : ICargoAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PttKargoAdapter> _logger;
    private readonly SimpleSoapClient _soapClient;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private string _shipmentServiceUrl = string.Empty;
    private string _trackingServiceUrl = string.Empty;
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private string _musteriId = string.Empty;
    private bool _isConfigured;

    private static readonly XNamespace PttNs = "http://ws.ptt.gov.tr/";
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(5, 5);

    public PttKargoAdapter(HttpClient httpClient, ILogger<PttKargoAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _soapClient = new SimpleSoapClient(httpClient, logger);

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
                        "[PttKargoAdapter] API retry {Attempt} after {Delay}ms",
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
                    _logger.LogWarning("[PttKargoAdapter] Circuit breaker OPENED for {Duration}s",
                        args.BreakDuration.TotalSeconds);
                    return default;
                }
            })
            .Build();
    }

    public CargoProvider Provider => CargoProvider.PttKargo;
    public bool SupportsCancellation => true;
    public bool SupportsLabelGeneration => true;
    public bool SupportsCashOnDelivery => true;
    public bool SupportsMultiParcel => true;

    public void Configure(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        _userName = credentials.GetValueOrDefault("UserName", "");
        _password = credentials.GetValueOrDefault("Password", "");
        _musteriId = credentials.GetValueOrDefault("MusteriId", "");

        _shipmentServiceUrl = credentials.GetValueOrDefault(
            "ShipmentServiceUrl",
            "https://pttws.ptt.gov.tr/PttVeriYukleme/services/Sorgu");
        _trackingServiceUrl = credentials.GetValueOrDefault(
            "TrackingServiceUrl",
            "https://pttws.ptt.gov.tr/GonderiTakip/services/Sorgu");

        _isConfigured = true;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException("PttKargoAdapter henuz konfigure edilmedi.");
    }

    // ── ICargoAdapter.IsAvailableAsync ──────────────────
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_isConfigured) return false;
        try
        {
            // Ping with minimal tracking query
            var body = new XElement(PttNs + "gonderiSorgula",
                AuthElement(),
                new XElement(PttNs + "barkodNo", "PING-TEST"));

            await _soapClient.SendAsync(
                _trackingServiceUrl,
                "http://ws.ptt.gov.tr/gonderiSorgula",
                body, ct);
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
            var body = new XElement(PttNs + "gonderiKaydet",
                AuthElement(),
                new XElement(PttNs + "GonderiVO",
                    El("referansNo", request.OrderId.ToString()),
                    El("aliciAdi", SecurityElement.Escape(request.RecipientName)),
                    El("aliciTelefon", SecurityElement.Escape(request.RecipientPhone)),
                    El("aliciAdres", SecurityElement.Escape(request.RecipientAddress.FullAddress)),
                    El("aliciIl", SecurityElement.Escape(request.RecipientAddress.City)),
                    El("aliciIlce", SecurityElement.Escape(request.RecipientAddress.District)),
                    El("aliciPostaKodu", SecurityElement.Escape(request.RecipientAddress.PostalCode ?? "")),
                    El("gondericIl", SecurityElement.Escape(request.SenderAddress.City)),
                    El("desi", request.Desi.ToString("F2")),
                    El("kg", request.Weight.ToString("F3")),
                    El("parcaSayisi", request.ParcelCount.ToString()),
                    El("aciklama", SecurityElement.Escape(request.Notes ?? "")),
                    El("kapidaOdeme", request.CodAmount.HasValue ? "E" : "H"),
                    El("kapidaOdemeTutar", request.CodAmount?.ToString("F2") ?? "0.00")));

            var result = await _soapClient.SendAsync(
                _shipmentServiceUrl,
                "http://ws.ptt.gov.tr/gonderiKaydet",
                body, ct);

            SimpleSoapClient.ThrowIfFault(result);

            var barkod = result.Descendants("barkodNo").FirstOrDefault()?.Value
                      ?? result.Descendants("gonderiNo").FirstOrDefault()?.Value;
            var shipmentId = result.Descendants("gonderiId").FirstOrDefault()?.Value ?? barkod;

            if (string.IsNullOrEmpty(barkod))
                return ShipmentResult.Failed("PTT Kargo barkod alinamadi");

            _logger.LogInformation("PTT Kargo gonderi olusturuldu: {Barkod}", barkod);
            return ShipmentResult.Succeeded(barkod, shipmentId ?? barkod);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PTT Kargo CreateShipment failed for order {OrderId}", request.OrderId);
            return ShipmentResult.Failed($"PTT Kargo hatasi: {ex.Message}");
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
            var body = new XElement(PttNs + "gonderiSorgula",
                AuthElement(),
                new XElement(PttNs + "barkodNo", SecurityElement.Escape(trackingNumber)));

            var result = await _soapClient.SendAsync(
                _trackingServiceUrl,
                "http://ws.ptt.gov.tr/gonderiSorgula",
                body, ct);

            SimpleSoapClient.ThrowIfFault(result);

            var statusCode = result.Descendants("durumKodu").FirstOrDefault()?.Value
                          ?? result.Descendants("durum").FirstOrDefault()?.Value ?? "";
            trackingResult.Status = MapPttStatus(statusCode);

            foreach (var hareket in result.Descendants("HareketVO"))
            {
                trackingResult.Events.Add(new TrackingEvent
                {
                    Timestamp = DateTime.TryParse(hareket.Element("tarih")?.Value, out var dt)
                        ? dt : DateTime.UtcNow,
                    Location = hareket.Element("birim")?.Value ?? "",
                    Description = hareket.Element("aciklama")?.Value ?? "",
                    Status = MapPttStatus(hareket.Element("durumKodu")?.Value ?? "")
                });
            }

            var tahminiTeslimat = result.Descendants("tahminiTeslimTarihi").FirstOrDefault()?.Value;
            if (DateTime.TryParse(tahminiTeslimat, out var eta))
                trackingResult.EstimatedDelivery = eta;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PTT Kargo tracking failed for {TrackingNumber}", trackingNumber);
            trackingResult.Status = CargoStatus.Created;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }

        return trackingResult;
    }

    // ── CancelShipmentAsync ─────────────────────────────
    public async Task<bool> CancelShipmentAsync(string shipmentId, CancellationToken ct = default)
    {
        EnsureConfigured();

        await _rateLimitSemaphore.WaitAsync(ct);
        try
        {
            var body = new XElement(PttNs + "gonderiIptal",
                AuthElement(),
                new XElement(PttNs + "gonderiNo", SecurityElement.Escape(shipmentId)));

            var result = await _soapClient.SendAsync(
                _shipmentServiceUrl,
                "http://ws.ptt.gov.tr/gonderiIptal",
                body, ct);

            SimpleSoapClient.ThrowIfFault(result);

            var sonuc = result.Descendants("sonuc").FirstOrDefault()?.Value ?? "";
            var basarili = sonuc.Equals("OK", StringComparison.OrdinalIgnoreCase)
                        || sonuc.Equals("1", StringComparison.OrdinalIgnoreCase)
                        || sonuc.Equals("BASARILI", StringComparison.OrdinalIgnoreCase);

            if (basarili)
                _logger.LogInformation("PTT Kargo gonderi iptal edildi: {ShipmentId}", shipmentId);
            else
                _logger.LogWarning("PTT Kargo iptal sonucu: {Sonuc} — ShipmentId: {ShipmentId}", sonuc, shipmentId);

            return basarili;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "PTT Kargo CancelShipment failed for {ShipmentId}", shipmentId);
            return false;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    // ── GetShipmentLabelAsync ───────────────────────────
    public async Task<LabelResult> GetShipmentLabelAsync(string shipmentId, CancellationToken ct = default)
    {
        EnsureConfigured();

        await _rateLimitSemaphore.WaitAsync(ct);
        try
        {
            var body = new XElement(PttNs + "etiketAl",
                AuthElement(),
                new XElement(PttNs + "gonderiNo", SecurityElement.Escape(shipmentId)));

            var result = await _soapClient.SendAsync(
                _shipmentServiceUrl,
                "http://ws.ptt.gov.tr/etiketAl",
                body, ct);

            SimpleSoapClient.ThrowIfFault(result);

            var base64Data = result.Descendants("etiketData").FirstOrDefault()?.Value
                          ?? result.Descendants("pdfData").FirstOrDefault()?.Value;

            if (string.IsNullOrEmpty(base64Data))
                throw new InvalidOperationException("PTT Kargo etiket verisi bos");

            return new LabelResult
            {
                Data = Convert.FromBase64String(base64Data),
                Format = LabelFormat.Pdf,
                FileName = $"ptt-label-{shipmentId}.pdf"
            };
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    // ── Helpers ─────────────────────────────────────────
    private XElement AuthElement() => new(PttNs + "kullaniciBilgi",
        El("kullaniciAdi", SecurityElement.Escape(_userName)),
        El("sifre", SecurityElement.Escape(_password)),
        El("musteriId", SecurityElement.Escape(_musteriId)));

    private static XElement El(string name, string value) => new(name, value);

    private static CargoStatus MapPttStatus(string code) => code.ToUpperInvariant() switch
    {
        "01" or "HAZIRLANDI" or "CREATED" => CargoStatus.Created,
        "02" or "TESLIM_ALINDI" or "ALINDI" => CargoStatus.PickedUp,
        "03" or "TRANSFERDE" or "YOLDA" => CargoStatus.InTransit,
        "04" or "DAGITIMDA" => CargoStatus.OutForDelivery,
        "05" or "TESLIM_EDILDI" or "DELIVERED" => CargoStatus.Delivered,
        "06" or "IADE" or "RETURNED" => CargoStatus.Returned,
        "07" or "IPTAL" or "CANCELLED" => CargoStatus.Cancelled,
        "08" or "SUBEDE" or "AT_BRANCH" => CargoStatus.AtBranch,
        "09" or "KAYIP" or "LOST" => CargoStatus.Lost,
        _ => CargoStatus.Created
    };
}
