using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// OpenCart platform adaptoru — mevcut OpenCartClient (MesTechStok.Core) mantigi
/// korunarak IIntegratorAdapter implement edildi.
/// OpenCart'ta kargo yonetimi yok (SupportsShipment = false).
/// </summary>
public class OpenCartAdapter : IIntegratorAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenCartAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private string _apiToken = string.Empty;
    private bool _isConfigured;

    public OpenCartAdapter(HttpClient httpClient, ILogger<OpenCartAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public string PlatformCode => nameof(PlatformType.OpenCart);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => false;

    private void ConfigureAuth(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        credentials.TryGetValue("ApiToken", out var apiToken);
        credentials.TryGetValue("BaseUrl", out var baseUrl);

        _apiToken = apiToken ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(baseUrl))
            _httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Oc-Restadmin-Id", _apiToken);
        _isConfigured = true;
    }

    public async Task<ConnectionTestResultDto> TestConnectionAsync(
        Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var sw = Stopwatch.StartNew();
        var result = new ConnectionTestResultDto { PlatformCode = PlatformCode };

        try
        {
            if (!credentials.ContainsKey("BaseUrl") || string.IsNullOrWhiteSpace(credentials["BaseUrl"]) ||
                !credentials.ContainsKey("ApiToken") || string.IsNullOrWhiteSpace(credentials["ApiToken"]))
            {
                result.ErrorMessage = "BaseUrl ve ApiToken alanlari zorunludur.";
                result.ResponseTime = sw.Elapsed;
                return result;
            }

            ConfigureAuth(credentials);

            var response = await _httpClient.GetAsync(
                new Uri($"/api/rest/products?limit=1&token={_apiToken}", UriKind.Relative), ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;
            sw.Stop();
            result.ResponseTime = sw.Elapsed;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);
                var totalCount = doc.RootElement.TryGetProperty("total", out var tc) ? tc.GetInt32() : 0;

                result.IsSuccess = true;
                result.ProductCount = totalCount;
                result.StoreName = $"OpenCart - {credentials["BaseUrl"]}";
            }
            else
            {
                result.ErrorMessage = $"OpenCart API hatasi: {response.StatusCode}";
            }
        }
        catch (TaskCanceledException)
        {
            result.ErrorMessage = "Baglanti zaman asimina ugradi.";
            result.ResponseTime = sw.Elapsed;
        }
        catch (HttpRequestException ex)
        {
            result.ErrorMessage = $"Baglanti hatasi: {ex.Message}";
            result.ResponseTime = sw.Elapsed;
        }

        _logger.LogInformation("OpenCart connection test: Success={Success}, Time={Time}ms",
            result.IsSuccess, result.ResponseTime.TotalMilliseconds);
        return result;
    }

    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PushProductAsync SKU: {SKU}", product.SKU);

        try
        {
            var payload = new
            {
                model = product.SKU,
                sku = product.SKU,
                quantity = product.Stock,
                price = product.SalePrice,
                product_description = new Dictionary<string, object>
                {
                    ["1"] = new { name = product.Name, description = product.Description ?? "" }
                },
                status = product.IsActive ? 1 : 0
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                new Uri($"/api/rest/products?token={_apiToken}", UriKind.Relative), content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("OpenCart PushProduct failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PushProduct exception: {SKU}", product.SKU);
            return false;
        }
    }

    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PullProductsAsync called");

        var products = new List<Product>();

        try
        {
            var page = 1;
            const int limit = 100;
            bool hasMore = true;

            while (hasMore)
            {
                var response = await _httpClient.GetAsync(
                    new Uri($"/api/rest/products?limit={limit}&page={page}&token={_apiToken}", UriKind.Relative), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode) break;

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("data", out var dataArr))
                {
                    var items = dataArr.EnumerateArray().ToList();
                    if (items.Count == 0) break;

                    foreach (var item in items)
                    {
                        products.Add(new Product
                        {
                            Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                            SKU = item.TryGetProperty("sku", out var s) ? s.GetString() ?? "" : "",
                            SalePrice = item.TryGetProperty("price", out var p) && decimal.TryParse(p.GetString(), out var pv) ? pv : 0,
                            Stock = item.TryGetProperty("quantity", out var q) && int.TryParse(q.GetString(), out var qv) ? qv : 0,
                            Description = item.TryGetProperty("description", out var d) ? d.GetString() : null
                        });
                    }

                    hasMore = items.Count == limit;
                }
                else
                {
                    hasMore = false;
                }

                page++;
            }

            _logger.LogInformation("OpenCart PullProducts: {Count} products retrieved", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PullProducts failed");
        }

        return products.AsReadOnly();
    }

    public async Task<bool> PushStockUpdateAsync(int productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PushStockUpdateAsync: ProductId={ProductId} qty={Qty}", productId, newStock);

        try
        {
            var payload = new { quantity = newStock };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(
                new Uri($"/api/rest/products/{productId}?token={_apiToken}", UriKind.Relative), content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("OpenCart StockUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart StockUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    public async Task<bool> PushPriceUpdateAsync(int productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("OpenCartAdapter.PushPriceUpdateAsync: ProductId={ProductId} price={Price}", productId, newPrice);

        try
        {
            var payload = new { price = newPrice };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(
                new Uri($"/api/rest/products/{productId}?token={_apiToken}", UriKind.Relative), content, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("OpenCart PriceUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenCart PriceUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "OpenCartAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
    }
}
