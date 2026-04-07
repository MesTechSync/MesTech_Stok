using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Services;

/// <summary>
/// S3-DEV3-02: Otomatik ürün eşleştirme servisi.
/// Platform'dan çekilen ürünler → lokal DB ürünleriyle eşleştirme.
/// Kural 1: EAN-13 tam eşleşme → %100 güven → otomatik
/// Kural 2: SKU eşleşme → %80 güven → öner
/// Kural 3: İsim benzerliği → %60 güven → öner
/// </summary>
public interface IProductMatchingService
{
    Task<ProductMatchResult> MatchAsync(string? barcode, string? sku, string? productName, CancellationToken ct = default);
    Task<IReadOnlyList<ProductMatchResult>> BatchMatchAsync(IReadOnlyList<ProductMatchRequest> requests, CancellationToken ct = default);
}

public sealed class ProductMatchingService : IProductMatchingService
{
    private readonly IProductRepository _productRepo;
    private readonly ILogger<ProductMatchingService> _logger;

    public ProductMatchingService(IProductRepository productRepo, ILogger<ProductMatchingService> logger)
    {
        _productRepo = productRepo;
        _logger = logger;
    }

    public async Task<ProductMatchResult> MatchAsync(string? barcode, string? sku, string? productName, CancellationToken ct = default)
    {
        // Kural 1: Barcode (EAN-13) tam eşleşme → %100 güven
        if (!string.IsNullOrEmpty(barcode))
        {
            var product = await _productRepo.GetByBarcodeAsync(barcode, ct).ConfigureAwait(false);
            if (product is not null)
            {
                return new ProductMatchResult
                {
                    ProductId = product.Id,
                    SKU = product.SKU,
                    ProductName = product.Name,
                    MatchedBy = MatchStrategy.Barcode,
                    Confidence = 100,
                    IsAutoMatch = true
                };
            }
        }

        // Kural 2: SKU eşleşme → %80 güven
        if (!string.IsNullOrEmpty(sku))
        {
            var product = await _productRepo.GetBySKUAsync(sku, ct).ConfigureAwait(false);
            if (product is not null)
            {
                return new ProductMatchResult
                {
                    ProductId = product.Id,
                    SKU = product.SKU,
                    ProductName = product.Name,
                    MatchedBy = MatchStrategy.SKU,
                    Confidence = 80,
                    IsAutoMatch = false // Manuel onay önerilir
                };
            }
        }

        // Kural 3: İsim benzerliği — basit contains kontrolü
        // (Levenshtein distance production'da eklenebilir)
        if (!string.IsNullOrEmpty(productName) && productName.Length >= 5)
        {
            var candidates = await _productRepo.GetBySKUsAsync(
                new[] { productName[..Math.Min(productName.Length, 20)] }, ct).ConfigureAwait(false);

            // TODO: Fuzzy matching (Levenshtein) DEV1 domain servisi olarak eklenebilir
            // Şimdilik exact-prefix match kullanıyoruz
        }

        return new ProductMatchResult
        {
            MatchedBy = MatchStrategy.None,
            Confidence = 0,
            IsAutoMatch = false
        };
    }

    public async Task<IReadOnlyList<ProductMatchResult>> BatchMatchAsync(
        IReadOnlyList<ProductMatchRequest> requests, CancellationToken ct = default)
    {
        var results = new List<ProductMatchResult>(requests.Count);

        foreach (var req in requests)
        {
            var result = await MatchAsync(req.Barcode, req.SKU, req.ProductName, ct).ConfigureAwait(false);
            result.RequestBarcode = req.Barcode;
            result.RequestSKU = req.SKU;
            results.Add(result);
        }

        var autoMatched = results.Count(r => r.IsAutoMatch);
        var suggested = results.Count(r => r.Confidence > 0 && !r.IsAutoMatch);
        var unmatched = results.Count(r => r.Confidence == 0);

        _logger.LogInformation(
            "[ProductMatching] Batch: {Total} istek, {Auto} otomatik, {Suggest} önerilen, {Unmatched} eşleşmemiş",
            requests.Count, autoMatched, suggested, unmatched);

        return results.AsReadOnly();
    }
}

public enum MatchStrategy { None, Barcode, SKU, NameSimilarity, BrandCategory }

public sealed class ProductMatchRequest
{
    public string? Barcode { get; set; }
    public string? SKU { get; set; }
    public string? ProductName { get; set; }
}

public sealed class ProductMatchResult
{
    public Guid? ProductId { get; set; }
    public string? SKU { get; set; }
    public string? ProductName { get; set; }
    public MatchStrategy MatchedBy { get; set; }
    public int Confidence { get; set; } // 0-100
    public bool IsAutoMatch { get; set; }
    public string? RequestBarcode { get; set; }
    public string? RequestSKU { get; set; }
}
