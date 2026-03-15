using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI.Accounting;

/// <summary>
/// Mock Advisory Agent V2 — gercek MESA OS olmadan test/gelistirme icin.
/// Statik ornek verilerle gunluk satis tavsiyesi olusturur.
/// </summary>
public class MockAdvisoryAgentV2 : IAdvisoryAgentV2
{
    private readonly ILogger<MockAdvisoryAgentV2> _logger;

    public MockAdvisoryAgentV2(ILogger<MockAdvisoryAgentV2> logger)
    {
        _logger = logger;
    }

    public Task<DailySalesAdvice> GenerateSalesAdviceAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK Advisory V2] Satis tavsiyesi istendi: tenant={TenantId}", tenantId);

        var advice = new DailySalesAdvice(
            TopRecommendations: new[]
            {
                new ProductRecommendation(
                    "Samsung Galaxy A55",
                    "Trendyol",
                    8999m,
                    "[MOCK] Son 7 gunde en cok satan urun — stok kontrolu onerilir."),
                new ProductRecommendation(
                    "Apple AirPods Pro 2",
                    "Hepsiburada",
                    6499m,
                    "[MOCK] Yuksek marjlı urun — reklam butcesi artirarak satis hacmi yukseltilmelidir."),
                new ProductRecommendation(
                    "Xiaomi Redmi Note 13",
                    "N11",
                    5199m,
                    "[MOCK] Rakip fiyatlari dusmus — fiyat guncelleme onerilir.")
            },
            Warnings: new[]
            {
                new ProductWarning(
                    "Logitech MX Master 3S",
                    "[MOCK] Son 30 gunde negatif marj (%−2.3)",
                    "Fiyat artir veya platformdan cek"),
                new ProductWarning(
                    "JBL Flip 6",
                    "[MOCK] Komisyon orani cok yuksek (%22.5)",
                    "Kategori degisikligi veya fiyat ayarlamasi yapilmali")
            },
            PlatformHealth: new[]
            {
                new PlatformHealth("Trendyol", "Normal", 12.5m, "[MOCK] Marj saglıklı — mevcut fiyatlandirmayi koruyun."),
                new PlatformHealth("Hepsiburada", "Yuksek", 18.3m, "[MOCK] Yuksek marj — reklam butcesi artirma firsati."),
                new PlatformHealth("N11", "Dusuk", 3.2m, "[MOCK] Dusuk marj — komisyon ve kargo giderlerini inceleyin."),
                new PlatformHealth("Ciceksepeti", "Negatif", -1.5m, "[MOCK] Negatif marj — fiyat artisi veya urun cekme onerilir."),
                new PlatformHealth("Amazon", "Normal", 10.8m, "[MOCK] Marj saglıklı — satis hacmi artirma firsati.")
            },
            GeneratedAt: DateTime.UtcNow);

        _logger.LogInformation(
            "[MOCK Advisory V2] Tavsiye olusturuldu: {RecCount} oneri, {WarnCount} uyari, {PlatformCount} platform",
            advice.TopRecommendations.Count, advice.Warnings.Count, advice.PlatformHealth.Count);

        return Task.FromResult(advice);
    }
}
