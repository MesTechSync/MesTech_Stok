using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI;

/// <summary>
/// Mock MESA AI servisi — gercek API cagrisi yapmaz, ornek veri doner.
/// Dalga 2'de RealMesaAIClient ile DI'dan swap edilecek.
/// </summary>
public class MockMesaAIService : IMesaAIService
{
    private readonly ILogger<MockMesaAIService> _logger;

    public MockMesaAIService(ILogger<MockMesaAIService> logger)
    {
        _logger = logger;
    }

    public Task<AiContentResult> GenerateProductDescriptionAsync(
        string sku, string productName, string? category,
        List<string>? imageUrls, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] AI icerik uretimi istendi: {SKU} - {Name}", sku, productName);

        var content = $"""
            {productName}

            Urun Ozellikleri:
            - Yuksek kaliteli malzeme
            - Ozenle tasarlanmis detaylar
            - Hizli kargo ile kapinizda
            - {category ?? "Genel"} kategorisinde en cok tercih edilen urunlerden

            Siparis notlari:
            Urun gorselleri temsilidir. Renk farkliliklari ekran ayarlarindan kaynaklanabilir.
            """;

        var metadata = new Dictionary<string, string>
        {
            ["seoTitle"] = $"{productName} - En Uygun Fiyat Garantisi",
            ["metaDescription"] = $"{productName} urununu en uygun fiyatla satin alin. Hizli kargo, kolay iade.",
            ["bulletPoint1"] = "Yuksek kaliteli malzeme",
            ["bulletPoint2"] = "Hizli ve guvenli kargo",
            ["bulletPoint3"] = "Kolay iade garantisi"
        };

        return Task.FromResult(new AiContentResult(true, content, null, metadata));
    }

    public Task<AiImageResult> GenerateProductImagesAsync(
        string sku, List<string> sourceImageUrls,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] AI gorsel uretimi istendi: {SKU}, {Count} kaynak gorsel",
            sku, sourceImageUrls.Count);

        var mockUrls = sourceImageUrls
            .Select((url, i) => $"https://mock-mesa-ai.local/generated/{sku}/img_{i + 1}.jpg")
            .ToList();

        return Task.FromResult(new AiImageResult(true, mockUrls, null));
    }

    public Task<AiPriceRecommendation> RecommendPriceAsync(
        string sku, decimal currentPrice,
        List<CompetitorPrice>? competitors,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] AI fiyat onerisi istendi: {SKU}, mevcut fiyat: {Price}",
            sku, currentPrice);

        var recommended = currentPrice * 0.95m;
        var min = currentPrice * 0.85m;
        var max = currentPrice * 1.10m;

        return Task.FromResult(new AiPriceRecommendation(
            true, Math.Round(recommended, 2),
            Math.Round(min, 2), Math.Round(max, 2),
            "Mock oneri: Mevcut fiyatin %5 altinda rekabetci fiyat onerilmektedir."));
    }

    public Task<AiStockPrediction> PredictStockAsync(
        string sku, int currentStock,
        List<StockHistoryPoint>? history,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] AI stok tahmini istendi: {SKU}, mevcut stok: {Stock}",
            sku, currentStock);

        var predictedDemand = Math.Max(10, currentStock / 3);
        var daysUntilStockout = currentStock > 0
            ? currentStock / Math.Max(1, predictedDemand / 30)
            : 0;

        return Task.FromResult(new AiStockPrediction(
            true, predictedDemand, predictedDemand * 2,
            daysUntilStockout,
            $"Mock tahmin: Gunluk ortalama {predictedDemand / 30} adet satis bekleniyor."));
    }

    public Task<AiCategoryMapping> SuggestCategoryAsync(
        string productName, string? description,
        string targetPlatform, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] AI kategori esleme istendi: {Name} -> {Platform}",
            productName, targetPlatform);

        return Task.FromResult(new AiCategoryMapping(
            true, "mock-cat-001", "Genel Urunler", 0.85));
    }
}
