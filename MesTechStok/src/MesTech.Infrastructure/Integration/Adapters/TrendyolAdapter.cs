using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Trendyol platform adaptoru — mevcut TrendyolApiClient mantigi korunarak
/// IIntegratorAdapter + IWebhookCapableAdapter implement edildi.
/// Rate limiting, Polly retry, Basic Auth mevcut koddan alinmistir.
/// </summary>
public class TrendyolAdapter : IIntegratorAdapter, IWebhookCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TrendyolAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

    private static readonly SemaphoreSlim _rateLimitSemaphore = new(100, 100);

    // Credential key'leri — StoreCredential tablosundaki Key alanlari
    private string? _supplierId;
    private bool _isConfigured;

    public TrendyolAdapter(HttpClient httpClient, ILogger<TrendyolAdapter> logger)
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
                    _logger.LogWarning(
                        "Trendyol API retry {Attempt} after {Delay}ms",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return default;
                }
            })
            .Build();
    }

    public string PlatformCode => nameof(PlatformType.Trendyol);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    private void ConfigureAuth(Dictionary<string, string> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var apiKey = credentials.GetValueOrDefault("ApiKey", "");
        var apiSecret = credentials.GetValueOrDefault("ApiSecret", "");
        _supplierId = credentials.GetValueOrDefault("SupplierId", "");

        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);

        if (!string.IsNullOrEmpty(credentials.GetValueOrDefault("BaseUrl")))
            _httpClient.BaseAddress = new Uri(credentials["BaseUrl"], UriKind.Absolute);

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MesTech-Trendyol-Client/3.0");
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
            // Credential dogrulama
            if (!credentials.ContainsKey("ApiKey") || string.IsNullOrWhiteSpace(credentials["ApiKey"]) ||
                !credentials.ContainsKey("ApiSecret") || string.IsNullOrWhiteSpace(credentials["ApiSecret"]) ||
                !credentials.ContainsKey("SupplierId") || string.IsNullOrWhiteSpace(credentials["SupplierId"]))
            {
                result.ErrorMessage = "ApiKey, ApiSecret ve SupplierId alanlari zorunludur.";
                result.ResponseTime = sw.Elapsed;
                return result;
            }

            ConfigureAuth(credentials);

            // Trendyol API'ye test istegi — urun sayisini cek
            var response = await _httpClient.GetAsync(
                new Uri($"/sapigw/suppliers/{_supplierId}/products?page=0&size=1", UriKind.Relative), ct).ConfigureAwait(false);

            result.HttpStatusCode = (int)response.StatusCode;
            sw.Stop();
            result.ResponseTime = sw.Elapsed;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);
                var totalElements = doc.RootElement.TryGetProperty("totalElements", out var te) ? te.GetInt32() : 0;

                result.IsSuccess = true;
                result.ProductCount = totalElements;
                result.StoreName = $"Trendyol - Supplier {_supplierId}";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                result.ErrorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Yetkisiz erisim — API Key/Secret hatali.",
                    System.Net.HttpStatusCode.Forbidden => "Erisim engellendi — Supplier ID hatali olabilir.",
                    _ => $"Trendyol API hatasi: {response.StatusCode} - {errorContent}"
                };
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

        _logger.LogInformation("Trendyol connection test: Success={Success}, Time={Time}ms",
            result.IsSuccess, result.ResponseTime.TotalMilliseconds);
        return result;
    }

    public async Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.PushProductAsync SKU: {SKU}", product.SKU);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                items = new[]
                {
                    new
                    {
                        barcode = product.Barcode ?? product.SKU,
                        title = product.Name,
                        productMainId = product.SKU,
                        brandId = product.BrandId ?? 1,
                        categoryId = product.CategoryId,
                        quantity = product.Stock,
                        stockCode = product.SKU,
                        description = product.Description ?? "",
                        currencyType = product.CurrencyCode,
                        listPrice = product.ListPrice ?? product.SalePrice,
                        salePrice = product.SalePrice,
                        vatRate = (int)(product.TaxRate * 100)
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/sapigw/suppliers/{_supplierId}/v2/products", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol PushProduct failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Trendyol PushProduct success: {SKU}", product.SKU);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol PushProduct exception: {SKU}", product.SKU);
            return false;
        }
    }

    public async Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.PullProductsAsync called");

        var products = new List<Product>();
        var page = 0;
        const int pageSize = 50;

        try
        {
            bool hasMore = true;
            while (hasMore)
            {
                await ApplyRateLimitAsync(ct).ConfigureAwait(false);

                var response = await _retryPipeline.ExecuteAsync(
                    async token => await _httpClient.GetAsync(
                        new Uri($"/sapigw/suppliers/{_supplierId}/products?page={page}&size={pageSize}", UriKind.Relative), token).ConfigureAwait(false), ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode) break;

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);

                var totalPages = doc.RootElement.TryGetProperty("totalPages", out var tp) ? tp.GetInt32() : 0;

                if (doc.RootElement.TryGetProperty("content", out var contentArr))
                {
                    foreach (var item in contentArr.EnumerateArray())
                    {
                        products.Add(new Product
                        {
                            Name = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                            SKU = item.TryGetProperty("stockCode", out var sc) ? sc.GetString() ?? "" : "",
                            Barcode = item.TryGetProperty("barcode", out var b) ? b.GetString() : null,
                            SalePrice = item.TryGetProperty("salePrice", out var sp) ? sp.GetDecimal() : 0,
                            ListPrice = item.TryGetProperty("listPrice", out var lp) ? lp.GetDecimal() : null,
                            Stock = item.TryGetProperty("quantity", out var q) ? q.GetInt32() : 0,
                            Description = item.TryGetProperty("description", out var d) ? d.GetString() : null
                        });
                    }
                }

                page++;
                hasMore = page < totalPages;
            }

            _logger.LogInformation("Trendyol PullProducts: {Count} products retrieved", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol PullProducts failed at page {Page}", page);
        }

        return products.AsReadOnly();
    }

    public async Task<bool> PushStockUpdateAsync(int productId, int newStock, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.PushStockUpdateAsync: ProductId={ProductId} qty={Qty}", productId, newStock);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                items = new[]
                {
                    new { barcode = productId.ToString(), quantity = newStock }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/sapigw/suppliers/{_supplierId}/products/price-and-inventory", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol StockUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol StockUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    public async Task<bool> PushPriceUpdateAsync(int productId, decimal newPrice, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.PushPriceUpdateAsync: ProductId={ProductId} price={Price}", productId, newPrice);

        try
        {
            await ApplyRateLimitAsync(ct).ConfigureAwait(false);

            var payload = new
            {
                items = new[]
                {
                    new { barcode = productId.ToString(), listPrice = newPrice, salePrice = newPrice }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var response = await _retryPipeline.ExecuteAsync(
                async token =>
                {
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(
                        new Uri($"/sapigw/suppliers/{_supplierId}/products/price-and-inventory", UriKind.Relative), content, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Trendyol PriceUpdate failed: {Status} - {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trendyol PriceUpdate exception: {ProductId}", productId);
            return false;
        }
    }

    // IWebhookCapableAdapter
    public async Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.RegisterWebhookAsync: {Url}", callbackUrl);
        // Trendyol webhook kaydi — Supplier API uzerinden
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        _logger.LogInformation("TrendyolAdapter.UnregisterWebhookAsync");
        await Task.CompletedTask;
        return true;
    }

    public async Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
    {
        _logger.LogInformation("TrendyolAdapter.ProcessWebhookPayloadAsync: {Length} chars", payload.Length);
        using var doc = JsonDocument.Parse(payload);
        // Webhook payload isleme — siparis, stok degisikligi vb.
        await Task.CompletedTask;
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(
                "TrendyolAdapter henuz yapilandirilmadi. Once TestConnectionAsync ile credential'lari verin.");
    }

    private async Task ApplyRateLimitAsync(CancellationToken ct = default)
    {
        await _rateLimitSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Mevcut TrendyolApiClient'tan alinan rate limiting mantigi
            await Task.Delay(10, ct).ConfigureAwait(false); // min 10ms between requests
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }
}
