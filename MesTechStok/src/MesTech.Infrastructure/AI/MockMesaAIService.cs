using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI;

/// <summary>
/// Mock MESA AI servisi — gercek API cagrisi yapmaz, ornek veri doner.
/// Kategori bazli gercekci Trendyol formati uretir.
/// Dalga 2+: RealMesaAIClient ile DI'dan swap edilecek.
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

        var content = BuildCategoryDescription(productName, category);
        var metadata = BuildSeoMetadata(productName, category);

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

        var competitorAvg = competitors is { Count: > 0 }
            ? competitors.Average(c => c.Price)
            : currentPrice;

        var recommended = Math.Round(competitorAvg * 0.97m, 2);
        var min = Math.Round(competitorAvg * 0.85m, 2);
        var max = Math.Round(competitorAvg * 1.05m, 2);

        var reasoning = competitors is { Count: > 0 }
            ? $"{competitors.Count} rakip analiz edildi. Ortalama fiyat: {competitorAvg:F2} TL. " +
              $"Rekabetci fiyat: {recommended:F2} TL (%3 altinda)."
            : "Rakip verisi yok. Mevcut fiyatin %3 altinda fiyat onerildi.";

        return Task.FromResult(new AiPriceRecommendation(
            true, recommended, min, max, reasoning));
    }

    public Task<AiStockPrediction> PredictStockAsync(
        string sku, int currentStock,
        List<StockHistoryPoint>? history,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] AI stok tahmini istendi: {SKU}, mevcut stok: {Stock}",
            sku, currentStock);

        var dailyAvg = history is { Count: >= 7 }
            ? (int)Math.Ceiling(history.TakeLast(7).Average(h => h.SoldCount))
            : Math.Max(1, currentStock / 30);

        var predictedDemand = dailyAvg * 30;
        var daysUntilStockout = dailyAvg > 0 ? currentStock / dailyAvg : 999;
        var reorderSuggestion = dailyAvg * 14;

        var reasoning = history is { Count: >= 7 }
            ? $"Son 7 gun verisi: gunluk ort. {dailyAvg} adet satis. " +
              $"Stok {daysUntilStockout} gun yeterli. 2 haftalik {reorderSuggestion} adet siparis onerilir."
            : $"Gecmis veri yetersiz. Tahmini gunluk satis: {dailyAvg} adet. " +
              $"2 haftalik {reorderSuggestion} adet siparis onerilir.";

        return Task.FromResult(new AiStockPrediction(
            true, predictedDemand, reorderSuggestion,
            daysUntilStockout, reasoning));
    }

    public Task<AiCategoryMapping> SuggestCategoryAsync(
        string productName, string? description,
        string targetPlatform, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] AI kategori esleme istendi: {Name} -> {Platform}",
            productName, targetPlatform);

        var (categoryId, categoryName, confidence) =
            GuessCategoryFromName(productName, targetPlatform);

        return Task.FromResult(new AiCategoryMapping(
            true, categoryId, categoryName, confidence));
    }

    public Task<AiReplyResult> SuggestReplyAsync(
        string messageBody, string? customerName,
        string? orderContext, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] AI yanit onerisi istendi: {Customer}", customerName ?? "Bilinmeyen");

        var greeting = string.IsNullOrEmpty(customerName)
            ? "Merhaba,"
            : $"Merhaba {customerName},";

        var suggestion = $"{greeting}\n\nMesajiniz icin tesekkur ederiz. " +
                         "Talebiniz incelenmektedir ve en kisa surede donuş yapilacaktir.\n\n" +
                         "Saygilarimizla,\nMesTech Musteri Hizmetleri";

        return Task.FromResult(new AiReplyResult(true, suggestion, null));
    }

    // ── Kategori bazli icerik uretimi ──

    private static string BuildCategoryDescription(
        string productName, string? category)
    {
        var cat = (category ?? "").ToLowerInvariant();

        if (cat.Contains("elektronik") || cat.Contains("telefon") || cat.Contains("bilgisayar"))
            return BuildElectronicsDescription(productName);

        if (cat.Contains("giyim") || cat.Contains("kiyafet") || cat.Contains("ayakkabi"))
            return BuildFashionDescription(productName);

        if (cat.Contains("gida") || cat.Contains("icecek") || cat.Contains("mutfak"))
            return BuildFoodDescription(productName);

        return BuildGenericDescription(productName, category);
    }

    private static string BuildElectronicsDescription(string name) =>
        $"""
        {name}

        Teknik Ozellikler:
        - Enerji verimli tasarim
        - Genis uyumluluk (USB-C / Bluetooth 5.0)
        - 2 yil resmi garanti
        - CE ve TSE belgeli

        Paket Icerigi:
        - 1x {name}
        - 1x Kullanim Kilavuzu
        - 1x Garanti Belgesi
        - 1x Sarj Kablosu

        Onemli Bilgiler:
        Urun resmi distributorluk garantisi altindadir. Fatura ile birlikte gonderilir.
        """;

    private static string BuildFashionDescription(string name) =>
        $"""
        {name}

        Urun Ozellikleri:
        - Rahat kalip, her vucuda uygun kesim
        - Nefes alabilen premium kumas
        - Renk solmaya karsi ozel isleme tabi tutulmustur
        - Makinede yikanabilir (30°C)

        Beden Tablosu:
        S (36-38) | M (38-40) | L (40-42) | XL (42-44) | XXL (44-46)

        Dikkat:
        Beden seciminde urun olculerine dikkat ediniz. Renk farkliliklari ekran ayarlarindan kaynaklanabilir.
        """;

    private static string BuildFoodDescription(string name) =>
        $"""
        {name}

        Urun Bilgileri:
        - Dogal icerik, katki maddesi bulunmamaktadir
        - Uygun kosullarda muhafaza ediniz
        - TSE ve ISO 22000 belgeli uretim tesisinde uretilmistir
        - Helal sertifikali

        Saklama Kosullari:
        Serin ve kuru ortamda, dogrudan gunes isinlarindan uzak muhafaza ediniz.

        Uretim ve son kullanma tarihi ambalaj uzerinde belirtilmistir.
        """;

    private static string BuildGenericDescription(
        string name, string? category) =>
        $"""
        {name}

        Urun Ozellikleri:
        - Yuksek kaliteli malzeme ile uretilmistir
        - Ozenle tasarlanmis detaylar
        - Hizli kargo ile kapinizda
        - {category ?? "Genel"} kategorisinde en cok tercih edilen urunlerden

        Neden Bu Urun?
        - Uygun fiyat garantisi
        - Hizli ve guvenli teslimat
        - Kolay iade imkani

        Siparis notlari:
        Urun gorselleri temsilidir. Renk farkliliklari ekran ayarlarindan kaynaklanabilir.
        """;

    private static Dictionary<string, string> BuildSeoMetadata(
        string productName, string? category)
    {
        var cat = category ?? "Genel";
        return new Dictionary<string, string>
        {
            ["seoTitle"] = $"{productName} | {cat} - Uygun Fiyat ve Hizli Kargo",
            ["metaDescription"] = $"{productName} urununu en uygun fiyatla satin alin. " +
                                  $"{cat} kategorisinde ozel indirimler. Hizli kargo, kolay iade.",
            ["bulletPoint1"] = "Yuksek kaliteli malzeme",
            ["bulletPoint2"] = "Hizli ve guvenli kargo",
            ["bulletPoint3"] = "Kolay iade garantisi",
            ["bulletPoint4"] = $"{cat} kategorisinde en cok tercih edilen",
            ["bulletPoint5"] = "Fatura ile birlikte gonderilir",
            ["keywords"] = $"{productName}, {cat}, uygun fiyat, hizli kargo, online alisveris"
        };
    }

    private static (string Id, string Name, double Confidence) GuessCategoryFromName(
        string productName, string platform)
    {
        var lower = productName.ToLowerInvariant();

        if (lower.Contains("telefon") || lower.Contains("phone"))
            return ($"{platform}-cat-elektronik-telefon", "Elektronik > Cep Telefonu", 0.92);
        if (lower.Contains("laptop") || lower.Contains("bilgisayar"))
            return ($"{platform}-cat-elektronik-bilgisayar", "Elektronik > Bilgisayar", 0.90);
        if (lower.Contains("tisort") || lower.Contains("gomlek") || lower.Contains("elbise"))
            return ($"{platform}-cat-giyim", "Giyim & Aksesuar", 0.88);
        if (lower.Contains("ayakkabi") || lower.Contains("bot") || lower.Contains("sneaker"))
            return ($"{platform}-cat-ayakkabi", "Ayakkabi & Canta", 0.87);

        return ($"{platform}-cat-genel", "Genel Urunler", 0.60);
    }
}
