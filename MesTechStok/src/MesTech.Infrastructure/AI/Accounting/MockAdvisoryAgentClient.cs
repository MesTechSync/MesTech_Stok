using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI.Accounting;

/// <summary>
/// Mock Advisory Agent — gercek MESA OS olmadan test/gelistirme icin.
/// Statik ornek verilerle gunluk brifing olusturur.
/// </summary>
public sealed class MockAdvisoryAgentClient : IAdvisoryAgentClient
{
    private readonly ILogger<MockAdvisoryAgentClient> _logger;

    public MockAdvisoryAgentClient(ILogger<MockAdvisoryAgentClient> logger)
    {
        _logger = logger;
    }

    public Task<DailyBriefing> GenerateBriefingAsync(
        Guid tenantId, DateTime date, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK Advisory] Gunluk brifing istendi: tenant={TenantId}, tarih={Date}",
            tenantId, date.ToString("yyyy-MM-dd"));

        var briefing = new DailyBriefing(
            Summary: $"[MOCK] {date:yyyy-MM-dd} gunluk mali ozet: " +
                     "3 yeni siparis, 2 iade talebi, kargo giderleri normal seviyelerde.",
            Recommendations: new[]
            {
                "[MOCK] Stok seviyesi dusuk urunleri yeniden siparis verin.",
                "[MOCK] Trendyol komisyon oranlarini kontrol edin.",
                "[MOCK] Haftalik kar-zarar raporu olusturun."
            },
            Alerts: new[]
            {
                "[MOCK] Uyari: 5 urunde stok sifir.",
                "[MOCK] Bilgi: Doviz kuru guncellendi."
            },
            TotalRevenue: 12500m,
            NetProfit: 2350m,
            OrderCount: 3);

        _logger.LogInformation(
            "[MOCK Advisory] Brifing olusturuldu: gelir={Revenue:N2}, kar={Profit:N2}",
            briefing.TotalRevenue, briefing.NetProfit);

        return Task.FromResult(briefing);
    }
}
