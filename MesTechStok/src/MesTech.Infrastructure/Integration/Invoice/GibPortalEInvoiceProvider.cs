using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// GIB e-Arsiv Portal IEInvoiceProvider implementasyonu (Dalga 10 E-10).
/// REST-based token auth ile GIB e-Arsiv Portal entegrasyonu.
/// Test: earsivportaltest.efatura.gov.tr, Prod: earsivportal.efatura.gov.tr.
/// Auth: FormUrlEncodedContent login (userid+password+assession_id+ression_id+cmd=login).
/// SendAsync: Token ile fatura olustur ve gonder.
/// GetPdfUrlAsync: Fatura PDF URL'si.
/// CancelAsync: Fatura iptal.
/// CheckVknMukellefAsync: VKN mukellef sorgusu (GIB public endpoint).
/// GetCreditBalanceAsync: e-Arsiv Portal ucretsiz (-1 = sinirsiz).
/// </summary>
public sealed class GibPortalEInvoiceProvider : IEInvoiceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GibPortalEInvoiceProvider> _logger;
    private readonly GibPortalEInvoiceOptions _options;

    private string? _token;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public string ProviderCode => "GibPortal";

    public GibPortalEInvoiceProvider(
        HttpClient httpClient,
        ILogger<GibPortalEInvoiceProvider> logger,
        IOptions<GibPortalEInvoiceOptions>? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new GibPortalEInvoiceOptions();
    }

    // ── IEInvoiceProvider ────────────────────────────────────────────────

    public async Task<EInvoiceSendResult> SendAsync(EInvoiceDocument document, CancellationToken ct = default)
    {
        _logger.LogInformation("GibPortalEInvoice SendAsync for ETTN {EttnNo}", document.EttnNo);

        try
        {
            await EnsureTokenAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                fatpicc = new
                {
                    fatpicc = new
                    {
                        faturaTipi = document.Scenario.ToString(),
                        uuid = document.GibUuid,
                        belgeNumarasi = document.EttnNo,
                        tarih = document.IssueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                        saat = document.IssueDate.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                        paraBirimi = document.CurrencyCode,
                        mpiccAliciAdi = document.BuyerTitle,
                        mpiccAliciVknTckn = document.BuyerVkn ?? string.Empty,
                        mpiccMalHizmetToplamTutari = document.LineExtensionAmount.ToString("F2", CultureInfo.InvariantCulture),
                        mpiccVergilerToplamTutari = document.TaxAmount.ToString("F2", CultureInfo.InvariantCulture),
                        mpiccOdenecekTutar = document.PayableAmount.ToString("F2", CultureInfo.InvariantCulture)
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, JsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/earsiv-services/dispatch")
            {
                Content = content
            };
            request.Headers.Add("Token", _token);

            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GibPortalEInvoice SendAsync failed: {Status} {Body}",
                    response.StatusCode, responseBody);
                return new EInvoiceSendResult(false, null, $"HTTP {response.StatusCode}: {responseBody}", 0);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var providerRef = root.TryGetProperty("uuid", out var uuidEl)
                ? uuidEl.GetString() ?? document.GibUuid
                : document.GibUuid;

            // e-Arsiv Portal is free (government portal) — 0 credit used
            _logger.LogInformation("GibPortalEInvoice SendAsync OK: ETTN={EttnNo} ProviderRef={Ref}",
                document.EttnNo, providerRef);
            return new EInvoiceSendResult(true, providerRef, null, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GibPortalEInvoice SendAsync exception for ETTN {EttnNo}", document.EttnNo);
            return new EInvoiceSendResult(false, null, ex.Message, 0);
        }
    }

    public async Task<string?> GetPdfUrlAsync(string providerRef, CancellationToken ct = default)
    {
        _logger.LogInformation("GibPortalEInvoice GetPdfUrlAsync for providerRef {ProviderRef}", providerRef);

        try
        {
            await EnsureTokenAsync(ct).ConfigureAwait(false);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{BaseUrl}/earsiv-services/download?token={Uri.EscapeDataString(_token!)}&ettn={Uri.EscapeDataString(providerRef)}");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GibPortalEInvoice GetPdfUrlAsync failed: {Status}", response.StatusCode);
                return null;
            }

            // The URL itself serves the PDF; return the download URL
            return $"{BaseUrl}/earsiv-services/download?token={Uri.EscapeDataString(_token!)}&ettn={Uri.EscapeDataString(providerRef)}&onizleme=Y";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GibPortalEInvoice GetPdfUrlAsync exception for {ProviderRef}", providerRef);
            return null;
        }
    }

    public async Task<bool> CancelAsync(string providerRef, string reason, CancellationToken ct = default)
    {
        _logger.LogInformation("GibPortalEInvoice CancelAsync for providerRef {ProviderRef}, reason: {Reason}",
            providerRef, reason);

        try
        {
            await EnsureTokenAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                ettn = providerRef,
                iptalNedeni = reason,
                belgeTarihi = DateTime.UtcNow.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            };

            var json = JsonSerializer.Serialize(payload, JsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/earsiv-services/dispatch")
            {
                Content = content
            };
            request.Headers.Add("Token", _token);

            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning("GibPortalEInvoice CancelAsync failed: {Status} {Body}",
                    response.StatusCode, body);
                return false;
            }

            _logger.LogInformation("GibPortalEInvoice CancelAsync OK for {ProviderRef}", providerRef);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GibPortalEInvoice CancelAsync exception for {ProviderRef}", providerRef);
            return false;
        }
    }

    /// <summary>
    /// GIB public mukellef sorgulama endpointi ile VKN kontrolu.
    /// e-Fatura + e-Arsiv mukellef durumunu doner.
    /// </summary>
    public async Task<VknMukellefResult> CheckVknMukellefAsync(string vkn, CancellationToken ct = default)
    {
        _logger.LogInformation("GibPortalEInvoice CheckVknMukellefAsync for VKN {Vkn}", vkn);

        try
        {
            // GIB public endpoint for VKN lookup (no auth required)
            var mukellefUrl = _options.MukellefQueryUrl;

            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "cmd", "SICIL_VEYA_MERBIS_SORGUSU" },
                { "callid", Guid.NewGuid().ToString() },
                { "pageName", "RG_BASITTASLAK" },
                { "jp", JsonSerializer.Serialize(new { vknTckn = vkn }, JsonOpts) }
            });

            using var response = await _httpClient.PostAsync(mukellefUrl, formContent, ct).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GibPortalEInvoice CheckVknMukellef failed: {Status}", response.StatusCode);
                return new VknMukellefResult(vkn, false, false, null, DateTime.UtcNow);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Parse mukellef info from GIB response
            var title = root.TryGetProperty("unvan", out var unvanEl) ? unvanEl.GetString() : null;

            // GIB returns mukellef bilgileri if found
            var isEInvoice = root.TryGetProperty("eFaturaMukellef", out var efEl)
                             && efEl.ValueKind == JsonValueKind.True;
            var isEArchive = root.TryGetProperty("eArsivMukellef", out var eaEl)
                             && eaEl.ValueKind == JsonValueKind.True;

            // If no specific fields, check if response indicates registration
            if (!isEInvoice && !isEArchive && title is not null)
            {
                // Any returned title means at least e-Arsiv capability (2022+ all companies)
                isEArchive = true;
            }

            return new VknMukellefResult(vkn, isEInvoice, isEArchive, title, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GibPortalEInvoice CheckVknMukellef exception for VKN {Vkn}", vkn);
            return new VknMukellefResult(vkn, false, false, null, null);
        }
    }

    /// <summary>
    /// GIB e-Arsiv Portal devlet portalidir — kontor/kredi sistemi yoktur.
    /// -1 doner (sinirsiz).
    /// </summary>
    public Task<int> GetCreditBalanceAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("GibPortalEInvoice GetCreditBalanceAsync — devlet portali, sinirsiz");
        return Task.FromResult(-1);
    }

    // ── Private: Token Management ────────────────────────────────────────

    private string BaseUrl => _options.BaseUrl.TrimEnd('/');

    private async Task EnsureTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
            return;

        _logger.LogInformation("GibPortalEInvoice: Acquiring new auth token from {BaseUrl}", BaseUrl);

        var loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "assession_id", Guid.NewGuid().ToString() },
            { "ression_id", Guid.NewGuid().ToString() },
            { "userid", _options.UserId },
            { "sifre", _options.Password },
            { "session_id", string.Empty },
            { "cmd", "login" },
            { "pageName", "R_LANDING" }
        });

        using var response = await _httpClient.PostAsync(
            $"{BaseUrl}/earsiv-services/assos-login", loginContent, ct).ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("GibPortalEInvoice login failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"GIB Portal login failed: HTTP {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("token", out var tokenEl))
        {
            _token = tokenEl.GetString()
                     ?? throw new InvalidOperationException("GIB Portal login returned null token");
        }
        else
        {
            throw new InvalidOperationException("GIB Portal login response missing 'token' field");
        }

        // GIB tokens typically expire in 30 minutes; refresh at 25 min
        _tokenExpiry = DateTime.UtcNow.AddMinutes(25);

        _logger.LogInformation("GibPortalEInvoice: Token acquired, expires at {Expiry}", _tokenExpiry);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Options
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// GIB e-Arsiv Portal REST provider options (Dalga 10 E-10).
/// Section: "Invoice:GibPortalEInvoice"
/// </summary>
public sealed class GibPortalEInvoiceOptions
{
    public const string Section = "Invoice:GibPortalEInvoice";

    /// <summary>
    /// GIB e-Arsiv Portal base URL.
    /// Test: https://earsivportaltest.efatura.gov.tr
    /// Prod: https://earsivportal.efatura.gov.tr
    /// </summary>
    public string BaseUrl { get; set; } = "https://earsivportaltest.efatura.gov.tr";

    /// <summary>VKN or TCKN for GIB portal login.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Portal password (Internet Vergi Dairesi password).</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>GIB public VKN mukellef sorgu URL (e-Arsiv dispatch).</summary>
    public string MukellefQueryUrl { get; set; } = "https://earsivportal.efatura.gov.tr/earsiv-services/dispatch";

    /// <summary>Whether the GIB Portal e-Invoice integration is enabled.</summary>
    public bool Enabled { get; set; } = false;
}
