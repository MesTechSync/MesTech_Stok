using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// PTT AVM platform adaptoru — foundation level.
/// Username + Password → Bearer token exchange (cached, 5-min buffer).
/// PTT kargo entegrasyonu tam implementasyon TODO H28.
/// </summary>
public class PttAvmAdapter : IIntegratorAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PttAvmAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Username/Password → Bearer token exchange
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private string _baseUrl = "https://apigw.pttavm.com";
    private string _tokenEndpoint = "https://apigw.pttavm.com/api/auth/login";
    private bool _isConfigured;

    // 5-minute safety buffer before actual expiry
    private static readonly TimeSpan TokenBuffer = TimeSpan.FromMinutes(5);

    public PttAvmAdapter(HttpClient httpClient, ILogger<PttAvmAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public string PlatformCode => nameof(PlatformType.PttAVM);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ═══════════════════════════════════════════
    // Username/Password Bearer Token Exchange
    // ═══════════════════════════════════════════

    /// <summary>
    /// Exchanges username/password for a Bearer token (cached with 5-min buffer).
    /// TODO H28: wire credentials from IStoreCredentialRepository.GetByStoreAndPlatformAsync
    /// </summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry - TokenBuffer)
            return _accessToken;

        // TODO H28: use _username + _password from IStoreCredentialRepository
        var loginPayload = JsonSerializer.Serialize(new
        {
            username = _username,
            password = _password
        }, _jsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
        request.Content = new StringContent(loginPayload, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
            cancellationToken: ct).ConfigureAwait(false);

        _accessToken = json.RootElement.GetProperty("token").GetString() ?? string.Empty;

        // PTT AVM tokens expire in 1 hour; parse if available, else default
        if (json.RootElement.TryGetProperty("expiresIn", out var expiresInEl))
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresInEl.GetInt32());
        else
            _tokenExpiry = DateTime.UtcNow.AddHours(1);

        _logger.LogInformation("PttAVM Bearer token refreshed — expires at {Expiry:u}", _tokenExpiry);
        return _accessToken;
    }

    private void SetBearerHeader(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private void ConfigureCredentials(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _username = credentials.GetValueOrDefault("Username", string.Empty);
        _password = credentials.GetValueOrDefault("Password", string.Empty);

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _baseUrl = credentials["BaseUrl"];
        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("TokenEndpoint")))
            _tokenEndpoint = credentials["TokenEndpoint"];

        _isConfigured = !string.IsNullOrWhiteSpace(_username) &&
                        !string.IsNullOrWhiteSpace(_password);
    }

    // ═══════════════════════════════════════════
    // IIntegratorAdapter — method stubs (TODO H28)
    // ═══════════════════════════════════════════

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        // TODO H28: POST /api/product/create — PTT AVM ürün listeleme
        _logger.LogWarning("PttAvmAdapter.PushProductAsync — TODO H28 (POST /api/product/create)");
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        // TODO H28: GET /api/product/list — sayfalı ürün çekme
        _logger.LogWarning("PttAvmAdapter.PullProductsAsync — TODO H28 (GET /api/product/list)");
        return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        // TODO H28: PUT /api/product/stock — stok güncelleme
        _logger.LogWarning("PttAvmAdapter.PushStockUpdateAsync — TODO H28 (PUT /api/product/stock)");
        return Task.FromResult(false);
    }

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        // TODO H28: PUT /api/product/price — fiyat güncelleme
        _logger.LogWarning("PttAvmAdapter.PushPriceUpdateAsync — TODO H28 (PUT /api/product/price)");
        return Task.FromResult(false);
    }

    public async Task<ConnectionTestResultDto> TestConnectionAsync(
        Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        var sw = Stopwatch.StartNew();
        var result = new ConnectionTestResultDto { PlatformCode = PlatformCode };

        try
        {
            ConfigureCredentials(credentials);

            if (!_isConfigured)
            {
                result.ErrorMessage = "PttAVM: Username veya Password eksik";
                return result;
            }

            // TODO H28: make a real probe call after obtaining token
            // e.g. GET /api/seller/info to verify credentials and retrieve store name
            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);
            result.IsSuccess = !string.IsNullOrEmpty(token);
            result.StoreName = "PTT AVM Satıcı (TODO H28 — gerçek mağaza adı)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PttAVM TestConnectionAsync başarısız");
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            sw.Stop();
            result.ResponseTime = sw.Elapsed;
        }

        return result;
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        // TODO H28: GET /api/category/list
        _logger.LogWarning("PttAvmAdapter.GetCategoriesAsync — TODO H28 (GET /api/category/list)");
        return Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());
    }
}
