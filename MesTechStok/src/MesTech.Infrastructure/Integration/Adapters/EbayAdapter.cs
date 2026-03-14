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
/// eBay platform adaptoru — foundation level.
/// OAuth2 Client Credentials Grant token (cached, 5-min buffer).
/// Tam implementasyon TODO H28.
/// </summary>
public class EbayAdapter : IIntegratorAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EbayAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // OAuth2 Client Credentials state
    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;
    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private string _tokenEndpoint = "https://api.ebay.com/identity/v1/oauth2/token";
    private bool _isConfigured;

    // 5-minute safety buffer before actual expiry
    private static readonly TimeSpan TokenBuffer = TimeSpan.FromMinutes(5);

    public EbayAdapter(HttpClient httpClient, ILogger<EbayAdapter> logger)
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

    public string PlatformCode => nameof(PlatformType.eBay);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => false;

    // ═══════════════════════════════════════════
    // OAuth2 Client Credentials Token Management
    // ═══════════════════════════════════════════

    /// <summary>
    /// Cached OAuth2 Client Credentials Grant token with 5-minute buffer.
    /// TODO H28: wire credentials from IStoreCredentialRepository.GetByStoreAndPlatformAsync
    /// </summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry - TokenBuffer)
            return _accessToken;

        // TODO H28: use _clientId + _clientSecret from IStoreCredentialRepository
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = "https://api.ebay.com/oauth/api_scope"
        });

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false),
            cancellationToken: ct).ConfigureAwait(false);

        _accessToken = json.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
        var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

        _logger.LogInformation("eBay OAuth2 token refreshed — expires in {Seconds}s", expiresIn);
        return _accessToken;
    }

    private void ConfigureCredentials(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _clientId = credentials.GetValueOrDefault("ClientId", string.Empty);
        _clientSecret = credentials.GetValueOrDefault("ClientSecret", string.Empty);

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("TokenEndpoint")))
            _tokenEndpoint = credentials["TokenEndpoint"];

        _isConfigured = !string.IsNullOrWhiteSpace(_clientId) &&
                        !string.IsNullOrWhiteSpace(_clientSecret);
    }

    // ═══════════════════════════════════════════
    // IIntegratorAdapter — method stubs (TODO H28)
    // ═══════════════════════════════════════════

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        // TODO H28: eBay Inventory API — PUT /sell/inventory/v1/inventory_item/{sku}
        _logger.LogWarning("EbayAdapter.PushProductAsync — TODO H28 (eBay Inventory API)");
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        // TODO H28: eBay Browse API — GET /buy/browse/v1/item_summary/search
        _logger.LogWarning("EbayAdapter.PullProductsAsync — TODO H28 (eBay Browse API)");
        return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        // TODO H28: eBay Inventory API — POST /sell/inventory/v1/bulk_update_price_quantity
        _logger.LogWarning("EbayAdapter.PushStockUpdateAsync — TODO H28 (bulk_update_price_quantity)");
        return Task.FromResult(false);
    }

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        // TODO H28: eBay Inventory API — POST /sell/inventory/v1/bulk_update_price_quantity
        _logger.LogWarning("EbayAdapter.PushPriceUpdateAsync — TODO H28 (bulk_update_price_quantity)");
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
                result.ErrorMessage = "eBay: ClientId veya ClientSecret eksik";
                return result;
            }

            // TODO H28: make a real probe call after obtaining token
            // e.g. GET /sell/inventory/v1/location to verify credentials
            var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);
            result.IsSuccess = !string.IsNullOrEmpty(token);
            result.StoreName = "eBay Store (TODO H28 — gerçek mağaza adı)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "eBay TestConnectionAsync başarısız");
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
        // TODO H28: eBay Taxonomy API — GET /commerce/taxonomy/v1/category_tree/{category_tree_id}
        _logger.LogWarning("EbayAdapter.GetCategoriesAsync — TODO H28 (eBay Taxonomy API)");
        return Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());
    }
}
