using System.Net.Http.Json;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace MesTech.Infrastructure.AI;

/// <summary>
/// Gercek MESA OS AI servisi — HTTP REST API uzerinden cagri yapar.
/// Feature flag: Mesa:UseProductionBridge=true olunca MockMesaAIService yerine bu kullanilir.
/// Demir Kural: MESA kopunca veya hata verince graceful fallback — MesTech calismaya devam eder.
/// Endpoint: Mesa:ApiUrl (appsettings: http://localhost:3000/api)
/// Circuit Breaker: 3 ardisik hata sonrasi 30s acik kalir, sonra half-open ile test eder.
/// </summary>
public sealed class ProductionMesaAIService : IMesaAIService
{
    private readonly HttpClient _httpClient;
    private readonly MockMesaAIService _mockFallback;
    private readonly ILogger<ProductionMesaAIService> _logger;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public ProductionMesaAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        MockMesaAIService mockFallback,
        ILogger<ProductionMesaAIService> logger)
    {
        _httpClient = httpClient;
        _mockFallback = mockFallback;
        _logger = logger;

        var baseUrl = configuration["Mesa:ApiUrl"] ?? "http://localhost:3000/api";
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        var apiKey = configuration["Mesa:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var timeoutSeconds = configuration.GetValue<int>("Mesa:TimeoutSeconds", 30);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        _circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, ts) => _logger.LogWarning(
                    "[MESA AI] Circuit OPEN — {Duration}s. Error: {Error}",
                    ts.TotalSeconds, ex.Message),
                onReset: () => _logger.LogInformation(
                    "[MESA AI] Circuit CLOSED — MESA OS baglantisi yeniden aktif"),
                onHalfOpen: () => _logger.LogInformation(
                    "[MESA AI] Circuit HALF-OPEN — test cagrisi yapiliyor"));
    }

    public async Task<AiContentResult> GenerateProductDescriptionAsync(
        string sku, string productName, string? category,
        List<string>? imageUrls, CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var payload = new
                {
                    sku,
                    productName,
                    category,
                    imageUrls = imageUrls ?? new List<string>()
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "v1/ai/content/description", payload, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA AI] GenerateDescription failed: {StatusCode} — falling back to mock (sku={SKU})",
                        response.StatusCode, sku);
                    return await _mockFallback.GenerateProductDescriptionAsync(
                        sku, productName, category, imageUrls, ct);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<MesaContentResponse>(cancellationToken: ct);

                if (result?.Content is null)
                {
                    _logger.LogWarning(
                        "[MESA AI] GenerateDescription deserialization failed — falling back (sku={SKU})", sku);
                    return await _mockFallback.GenerateProductDescriptionAsync(
                        sku, productName, category, imageUrls, ct);
                }

                _logger.LogInformation(
                    "[MESA AI] GenerateDescription basarili: sku={SKU}, len={Len}",
                    sku, result.Content.Length);

                return new AiContentResult(true, result.Content, null, null);
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex,
                "[MESA AI] MESA OS unreachable, falling back to mock (GenerateDescription, sku={SKU})", sku);
            return await _mockFallback.GenerateProductDescriptionAsync(
                sku, productName, category, imageUrls, ct);
        }
    }

    public async Task<AiImageResult> GenerateProductImagesAsync(
        string sku, List<string> sourceImageUrls, CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var payload = new { sku, sourceImageUrls };

                var response = await _httpClient.PostAsJsonAsync(
                    "v1/ai/images/generate", payload, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA AI] GenerateImages failed: {StatusCode} — falling back (sku={SKU})",
                        response.StatusCode, sku);
                    return await _mockFallback.GenerateProductImagesAsync(sku, sourceImageUrls, ct);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<MesaImageResponse>(cancellationToken: ct);

                if (result?.ImageUrls is null or { Count: 0 })
                {
                    _logger.LogWarning(
                        "[MESA AI] GenerateImages empty response — falling back (sku={SKU})", sku);
                    return await _mockFallback.GenerateProductImagesAsync(sku, sourceImageUrls, ct);
                }

                _logger.LogInformation(
                    "[MESA AI] GenerateImages basarili: sku={SKU}, count={Count}",
                    sku, result.ImageUrls.Count);

                return new AiImageResult(true, result.ImageUrls, null);
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex,
                "[MESA AI] MESA OS unreachable, falling back to mock (GenerateImages, sku={SKU})", sku);
            return await _mockFallback.GenerateProductImagesAsync(sku, sourceImageUrls, ct);
        }
    }

    public async Task<AiPriceRecommendation> RecommendPriceAsync(
        string sku, decimal currentPrice, List<CompetitorPrice>? competitors,
        CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var payload = new
                {
                    sku,
                    currentPrice,
                    competitors = competitors ?? new List<CompetitorPrice>()
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "v1/ai/price/recommend", payload, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA AI] RecommendPrice failed: {StatusCode} — falling back (sku={SKU})",
                        response.StatusCode, sku);
                    return await _mockFallback.RecommendPriceAsync(sku, currentPrice, competitors, ct);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<MesaPriceResponse>(cancellationToken: ct);

                if (result?.RecommendedPrice is null)
                {
                    _logger.LogWarning(
                        "[MESA AI] RecommendPrice null response — falling back (sku={SKU})", sku);
                    return await _mockFallback.RecommendPriceAsync(sku, currentPrice, competitors, ct);
                }

                _logger.LogInformation(
                    "[MESA AI] RecommendPrice basarili: sku={SKU}, oneri={Price:F2}",
                    sku, result.RecommendedPrice.Value);

                return new AiPriceRecommendation(
                    true,
                    result.RecommendedPrice.Value,
                    result.MinPrice ?? Math.Round(result.RecommendedPrice.Value * 0.85m, 2),
                    result.MaxPrice ?? Math.Round(result.RecommendedPrice.Value * 1.10m, 2),
                    result.Reasoning ?? "MESA AI fiyat onerisi");
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex,
                "[MESA AI] MESA OS unreachable, falling back to mock (RecommendPrice, sku={SKU})", sku);
            return await _mockFallback.RecommendPriceAsync(sku, currentPrice, competitors, ct);
        }
    }

    public async Task<AiStockPrediction> PredictStockAsync(
        string sku, int currentStock, List<StockHistoryPoint>? history,
        CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var payload = new
                {
                    sku,
                    currentStock,
                    history = history ?? new List<StockHistoryPoint>()
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "v1/ai/stock/predict", payload, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA AI] PredictStock failed: {StatusCode} — falling back (sku={SKU})",
                        response.StatusCode, sku);
                    return await _mockFallback.PredictStockAsync(sku, currentStock, history, ct);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<MesaStockPredictionResponse>(cancellationToken: ct);

                if (result is null)
                {
                    _logger.LogWarning(
                        "[MESA AI] PredictStock null response — falling back (sku={SKU})", sku);
                    return await _mockFallback.PredictStockAsync(sku, currentStock, history, ct);
                }

                _logger.LogInformation(
                    "[MESA AI] PredictStock basarili: sku={SKU}, demand={Demand}, days={Days}",
                    sku, result.PredictedDemand, result.DaysUntilStockout);

                return new AiStockPrediction(
                    true,
                    result.PredictedDemand,
                    result.ReorderSuggestion,
                    result.DaysUntilStockout,
                    result.Reasoning ?? "MESA AI stok tahmini");
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex,
                "[MESA AI] MESA OS unreachable, falling back to mock (PredictStock, sku={SKU})", sku);
            return await _mockFallback.PredictStockAsync(sku, currentStock, history, ct);
        }
    }

    public async Task<AiCategoryMapping> SuggestCategoryAsync(
        string productName, string? description,
        string targetPlatform, CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var payload = new { productName, description, targetPlatform };

                var response = await _httpClient.PostAsJsonAsync(
                    "v1/ai/category/suggest", payload, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA AI] SuggestCategory failed: {StatusCode} — falling back (product={Name})",
                        response.StatusCode, productName);
                    return await _mockFallback.SuggestCategoryAsync(
                        productName, description, targetPlatform, ct);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<MesaCategoryResponse>(cancellationToken: ct);

                if (result is null)
                {
                    _logger.LogWarning(
                        "[MESA AI] SuggestCategory null response — falling back (product={Name})", productName);
                    return await _mockFallback.SuggestCategoryAsync(
                        productName, description, targetPlatform, ct);
                }

                _logger.LogInformation(
                    "[MESA AI] SuggestCategory basarili: {Name} -> {CategoryId} ({Confidence:P0})",
                    productName, result.CategoryId, result.Confidence);

                return new AiCategoryMapping(
                    true,
                    result.CategoryId ?? $"{targetPlatform}-cat-genel",
                    result.CategoryName ?? "Genel Urunler",
                    result.Confidence);
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex,
                "[MESA AI] MESA OS unreachable, falling back to mock (SuggestCategory, product={Name})",
                productName);
            return await _mockFallback.SuggestCategoryAsync(
                productName, description, targetPlatform, ct);
        }
    }

    public async Task<AiReplyResult> SuggestReplyAsync(
        string messageBody, string? customerName,
        string? orderContext, CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var payload = new { messageBody, customerName, orderContext };

                var response = await _httpClient.PostAsJsonAsync(
                    "v1/ai/crm/suggest-reply", payload, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA AI] SuggestReply failed: {StatusCode} — falling back (customer={Customer})",
                        response.StatusCode, customerName);
                    return await _mockFallback.SuggestReplyAsync(
                        messageBody, customerName, orderContext, ct);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<MesaReplyResponse>(cancellationToken: ct);

                if (result?.SuggestedReply is null)
                {
                    _logger.LogWarning(
                        "[MESA AI] SuggestReply null response — falling back (customer={Customer})", customerName);
                    return await _mockFallback.SuggestReplyAsync(
                        messageBody, customerName, orderContext, ct);
                }

                _logger.LogInformation(
                    "[MESA AI] SuggestReply basarili: customer={Customer}, len={Len}",
                    customerName, result.SuggestedReply.Length);

                return new AiReplyResult(true, result.SuggestedReply, null);
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex,
                "[MESA AI] MESA OS unreachable, falling back to mock (SuggestReply, customer={Customer})",
                customerName);
            return await _mockFallback.SuggestReplyAsync(
                messageBody, customerName, orderContext, ct);
        }
    }
}

// ── MESA AI Response DTOs ──

/// <summary>MESA OS /api/v1/ai/content/description yanit modeli.</summary>
public record MesaContentResponse(string? Content);

/// <summary>MESA OS /api/v1/ai/images/generate yanit modeli.</summary>
public record MesaImageResponse(List<string>? ImageUrls);

/// <summary>MESA OS /api/v1/ai/price/recommend yanit modeli.</summary>
public record MesaPriceResponse(
    decimal? RecommendedPrice,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Reasoning);

/// <summary>MESA OS /api/v1/ai/stock/predict yanit modeli.</summary>
public record MesaStockPredictionResponse(
    int PredictedDemand,
    int ReorderSuggestion,
    int DaysUntilStockout,
    string? Reasoning);

/// <summary>MESA OS /api/v1/ai/category/suggest yanit modeli.</summary>
public record MesaCategoryResponse(
    string? CategoryId,
    string? CategoryName,
    double Confidence);

/// <summary>MESA OS /api/v1/ai/crm/suggest-reply yanit modeli.</summary>
public record MesaReplyResponse(string? SuggestedReply);
