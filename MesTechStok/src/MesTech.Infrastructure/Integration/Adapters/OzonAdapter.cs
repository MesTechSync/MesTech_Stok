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
/// Ozon platform adaptoru — foundation level.
/// Client-Id + Api-Key header auth (no Bearer token exchange).
/// FBO/FBS modeli tam implementasyon TODO H28.
/// </summary>
public class OzonAdapter : IIntegratorAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OzonAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Ozon uses header-based auth — no token exchange
    private string _clientId = string.Empty;
    private string _apiKey = string.Empty;
    private string _baseUrl = "https://api-seller.ozon.ru";
    private bool _isConfigured;

    private const string ClientIdHeader = "Client-Id";
    private const string ApiKeyHeader = "Api-Key";

    public OzonAdapter(HttpClient httpClient, ILogger<OzonAdapter> logger)
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

    public string PlatformCode => nameof(PlatformType.Ozon);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // ═══════════════════════════════════════════
    // Header-based Auth Request Builder
    // ═══════════════════════════════════════════

    /// <summary>
    /// Builds an HttpRequestMessage with Ozon Client-Id and Api-Key headers.
    /// TODO H28: wire credentials from IStoreCredentialRepository.GetByStoreAndPlatformAsync
    /// </summary>
    private HttpRequestMessage BuildRequest(HttpMethod method, string relativeUrl)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{relativeUrl}");
        request.Headers.TryAddWithoutValidation(ClientIdHeader, _clientId);
        request.Headers.TryAddWithoutValidation(ApiKeyHeader, _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private void ConfigureCredentials(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _clientId = credentials.GetValueOrDefault("ClientId", string.Empty);
        _apiKey = credentials.GetValueOrDefault("ApiKey", string.Empty);

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _baseUrl = credentials["BaseUrl"];

        _isConfigured = !string.IsNullOrWhiteSpace(_clientId) &&
                        !string.IsNullOrWhiteSpace(_apiKey);
    }

    // ═══════════════════════════════════════════
    // IIntegratorAdapter — method stubs (TODO H28)
    // ═══════════════════════════════════════════

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        // TODO H28: POST /v2/product/import — FBO/FBS listing creation
        _logger.LogWarning("OzonAdapter.PushProductAsync — TODO H28 (POST /v2/product/import)");
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        // TODO H28: POST /v2/product/list + POST /v2/product/info/list
        _logger.LogWarning("OzonAdapter.PullProductsAsync — TODO H28 (POST /v2/product/list)");
        return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        // TODO H28: POST /v1/product/import/stocks (FBO) or /v2/products/stocks (FBS)
        _logger.LogWarning("OzonAdapter.PushStockUpdateAsync — TODO H28 (POST /v1/product/import/stocks)");
        return Task.FromResult(false);
    }

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        // TODO H28: POST /v1/product/import/prices
        _logger.LogWarning("OzonAdapter.PushPriceUpdateAsync — TODO H28 (POST /v1/product/import/prices)");
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
                result.ErrorMessage = "Ozon: ClientId veya ApiKey eksik";
                return result;
            }

            // TODO H28: make a real probe call — e.g. POST /v1/product/list with empty filter
            // For now, validate that we can build a request with headers
            using var probe = BuildRequest(HttpMethod.Get, "/v1/product/list");
            result.IsSuccess = probe.Headers.Contains(ClientIdHeader) &&
                               probe.Headers.Contains(ApiKeyHeader);
            result.StoreName = "Ozon Seller (TODO H28 — gerçek mağaza adı)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ozon TestConnectionAsync başarısız");
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
        // TODO H28: GET /v2/category/tree
        _logger.LogWarning("OzonAdapter.GetCategoriesAsync — TODO H28 (GET /v2/category/tree)");
        return Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());
    }
}
