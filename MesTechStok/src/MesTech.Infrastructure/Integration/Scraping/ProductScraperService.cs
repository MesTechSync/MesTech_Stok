using System.Text.Json;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Scraping;

/// <summary>
/// URL-bazli urun bilgi servisi — platform URL'sinden urun detayini cekmek icin.
/// Web scraping YAPMAZ — platform API endpointlerini kullanir.
/// Desteklenen platformlar: Trendyol, Hepsiburada, N11, Ciceksepeti, Pazarama.
/// </summary>
public sealed class ProductScraperService : IProductScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductScraperService> _logger;

    // Platform URL detection map
    private static readonly Dictionary<string, string> PlatformDomainMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["trendyol.com"] = "Trendyol",
        ["ty.gl"] = "Trendyol",
        ["hepsiburada.com"] = "Hepsiburada",
        ["n11.com"] = "N11",
        ["ciceksepeti.com"] = "Ciceksepeti",
        ["pazarama.com"] = "Pazarama"
    };

    public ProductScraperService(HttpClient httpClient, ILogger<ProductScraperService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ScrapedProductDto?> ScrapeFromUrlAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("ScrapeFromUrlAsync called with empty URL");
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("ScrapeFromUrlAsync called with invalid URL: {Url}", url);
            return null;
        }

        var platform = DetectPlatform(uri.Host);
        if (platform is null)
        {
            _logger.LogWarning("Unsupported platform for URL: {Url}", url);
            return null;
        }

        _logger.LogInformation("Scraping product from {Platform} URL: {Url}", platform, url);

        try
        {
            return await FetchProductAsync(platform, uri, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scrape product from {Platform} URL: {Url}", platform, url);
            return null;
        }
    }

    /// <summary>
    /// Host'tan platform tespit eder. Alt domainleri de destekler (www.trendyol.com, m.hepsiburada.com).
    /// </summary>
    internal static string? DetectPlatform(string host)
    {
        // Check exact match first
        if (PlatformDomainMap.TryGetValue(host, out var platform))
            return platform;

        // Check if host ends with ".{domain}" (subdomain support)
        foreach (var (domain, platformName) in PlatformDomainMap)
        {
            if (host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase))
                return platformName;
        }

        return null;
    }

    private async Task<ScrapedProductDto?> FetchProductAsync(string platform, Uri uri, CancellationToken ct)
    {
        // Extract product identifier from URL path
        var productId = ExtractProductId(platform, uri);
        if (string.IsNullOrEmpty(productId))
        {
            _logger.LogWarning("Could not extract product ID from {Platform} URL: {Uri}", platform, uri);
            return null;
        }

        // Build platform-specific API URL
        var apiUrl = BuildApiUrl(platform, productId);
        if (apiUrl is null)
        {
            _logger.LogWarning("Could not build API URL for {Platform} product: {ProductId}", platform, productId);
            return null;
        }

        var response = await _httpClient.GetAsync(apiUrl, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("API call to {Platform} failed: {Status} for product {ProductId}",
                platform, response.StatusCode, productId);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return ParseProductResponse(platform, json);
    }

    /// <summary>
    /// URL path'inden urun ID'sini cikarir.
    /// Trendyol: /marka/urun-adi-p-123456 -> "123456"
    /// HB: /urun-adi-p-HBCV000001 -> "HBCV000001"
    /// N11: /urun/urun-adi-123456 -> "123456"
    /// CS: /urun/detay/123456 -> "123456"
    /// Pazarama: /urun/urun-adi-123456 -> "123456"
    /// </summary>
    internal static string? ExtractProductId(string platform, Uri uri)
    {
        var path = uri.AbsolutePath.TrimEnd('/');
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return platform switch
        {
            "Trendyol" => ExtractTrendyolProductId(path),
            "Hepsiburada" => ExtractHepsiburadaProductId(path),
            "N11" => ExtractLastSegmentId(segments),
            "Ciceksepeti" => ExtractLastSegmentId(segments),
            "Pazarama" => ExtractLastSegmentId(segments),
            _ => null
        };
    }

    private static string? ExtractTrendyolProductId(string path)
    {
        // Trendyol pattern: /brand/product-name-p-123456
        var pIdx = path.LastIndexOf("-p-", StringComparison.Ordinal);
        if (pIdx < 0) return null;
        var afterP = path[(pIdx + 3)..];
        // Take only digits (may have query params after)
        var idEnd = 0;
        while (idEnd < afterP.Length && char.IsDigit(afterP[idEnd])) idEnd++;
        return idEnd > 0 ? afterP[..idEnd] : null;
    }

    private static string? ExtractHepsiburadaProductId(string path)
    {
        // HB pattern: /product-name-p-HBCV000001
        var pIdx = path.LastIndexOf("-p-", StringComparison.Ordinal);
        if (pIdx < 0) return null;
        return path[(pIdx + 3)..];
    }

    private static string? ExtractLastSegmentId(string[] segments)
    {
        if (segments.Length == 0) return null;
        var last = segments[^1];
        // Try to extract numeric ID from end: "product-name-123456" -> "123456"
        var dashIdx = last.LastIndexOf('-');
        if (dashIdx >= 0)
        {
            var candidate = last[(dashIdx + 1)..];
            if (candidate.All(char.IsDigit) && candidate.Length > 0)
                return candidate;
        }
        return last;
    }

    private static string? BuildApiUrl(string platform, string productId)
    {
        return platform switch
        {
            "Trendyol" => $"https://apigw.trendyol.com/integration/product/products/{productId}",
            "Hepsiburada" => $"https://api.hepsiburada.com/product/api/products/{productId}",
            "N11" => $"https://api.n11.com/rest/products/{productId}",
            "Ciceksepeti" => $"https://api.ciceksepeti.com/products/{productId}",
            "Pazarama" => $"https://api.pazarama.com/marketplace/products/{productId}",
            _ => null
        };
    }

    private ScrapedProductDto? ParseProductResponse(string platform, string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var title = root.TryGetProperty("name", out var n) ? n.GetString()
                : root.TryGetProperty("title", out var t) ? t.GetString()
                : root.TryGetProperty("productName", out var pn) ? pn.GetString()
                : null;

            if (string.IsNullOrEmpty(title)) return null;

            var price = root.TryGetProperty("price", out var p) ? p.GetDecimal()
                : root.TryGetProperty("salePrice", out var sp) ? sp.GetDecimal()
                : 0m;

            var imageUrl = root.TryGetProperty("imageUrl", out var img) ? img.GetString()
                : root.TryGetProperty("images", out var imgs) && imgs.ValueKind == JsonValueKind.Array
                    ? imgs.EnumerateArray().FirstOrDefault().GetString()
                : null;

            var barcode = root.TryGetProperty("barcode", out var b) ? b.GetString()
                : root.TryGetProperty("sku", out var s) ? s.GetString()
                : null;

            var categoryPath = root.TryGetProperty("categoryPath", out var cp) ? cp.GetString()
                : root.TryGetProperty("category", out var c) ? c.GetString()
                : null;

            var brand = root.TryGetProperty("brand", out var br) ? br.GetString()
                : root.TryGetProperty("brandName", out var bn) ? bn.GetString()
                : null;

            var description = root.TryGetProperty("description", out var d) ? d.GetString() : null;

            return new ScrapedProductDto(title, price, imageUrl, barcode, platform, categoryPath, brand, description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse {Platform} product JSON response", platform);
            return null;
        }
    }
}
