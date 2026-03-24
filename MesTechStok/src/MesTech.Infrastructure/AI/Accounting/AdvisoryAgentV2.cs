using System.Globalization;
using System.Net.Http.Json;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI.Accounting;

/// <summary>
/// "Bugun ne sat" — gunluk satis tavsiye arayuzu.
/// Son 30 gunluk ProfitReport + CommissionRecord verilerini analiz eder,
/// urun bazinda platform marjini hesaplar ve satis onerileri olusturur.
/// </summary>
public interface IAdvisoryAgentV2
{
    Task<DailySalesAdvice> GenerateSalesAdviceAsync(Guid tenantId, CancellationToken ct = default);
}

/// <summary>
/// Gunluk satis tavsiye sonucu.
/// </summary>
public record DailySalesAdvice(
    IReadOnlyList<ProductRecommendation> TopRecommendations,
    IReadOnlyList<ProductWarning> Warnings,
    IReadOnlyList<PlatformHealth> PlatformHealth,
    DateTime GeneratedAt);

public record ProductRecommendation(string ProductName, string Platform, decimal SuggestedPrice, string Reason);
public record ProductWarning(string ProductName, string Reason, string Action);
public record PlatformHealth(string Platform, string MarginTrend, decimal AvgMargin, string Suggestion);

/// <summary>
/// MESA OS Advisory Agent V2 gercek HTTP istemcisi.
/// POST localhost:3101/api/v1/accounting/advisory/sales
/// Demir Kural #12: MESA kopunce kural tabanlı basit tavsiye uretir (AI'siz fallback).
/// </summary>
public class AdvisoryAgentV2 : IAdvisoryAgentV2
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IProfitReportRepository _profitReportRepository;
    private readonly ICommissionRecordRepository _commissionRepository;
    private readonly IProductRepository _productRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AdvisoryAgentV2> _logger;

    public AdvisoryAgentV2(
        HttpClient httpClient,
        IConfiguration configuration,
        IProfitReportRepository profitReportRepository,
        ICommissionRecordRepository commissionRepository,
        IProductRepository productRepository,
        ITenantProvider tenantProvider,
        ILogger<AdvisoryAgentV2> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _profitReportRepository = profitReportRepository;
        _commissionRepository = commissionRepository;
        _productRepository = productRepository;
        _tenantProvider = tenantProvider;
        _logger = logger;

        var baseUrl = _configuration["Mesa:Accounting:BaseUrl"] ?? "http://localhost:3101";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<DailySalesAdvice> GenerateSalesAdviceAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Advisory V2] Satis tavsiyesi hazirlaniyor: tenant={TenantId}", tenantId);

        try
        {
            // Son 30 gun verilerini topla
            var analysisData = await CollectAnalysisDataAsync(tenantId, ct);

            // MESA AI'ya gonder
            var payload = new
            {
                tenantId,
                date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                profitReports = analysisData.Reports.Select(r => new
                {
                    r.Platform,
                    r.TotalRevenue,
                    r.TotalCommission,
                    r.TotalCargo,
                    r.NetProfit,
                    r.Period
                }),
                commissions = analysisData.Commissions.Select(c => new
                {
                    c.Platform,
                    c.GrossAmount,
                    c.CommissionRate,
                    c.CommissionAmount,
                    c.Category
                }),
                productCount = analysisData.ProductCount
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/accounting/advisory/sales", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[Advisory V2] MESA satis tavsiyesi basarisiz: {StatusCode}", response.StatusCode);
                return GenerateRuleBasedAdvice(analysisData);
            }

            var result = await response.Content.ReadFromJsonAsync<MesaSalesAdviceResponse>(
                cancellationToken: ct);

            if (result is null)
            {
                _logger.LogWarning("[Advisory V2] MESA yanit deserialization basarisiz");
                return GenerateRuleBasedAdvice(analysisData);
            }

            _logger.LogInformation(
                "[Advisory V2] MESA satis tavsiyesi alindi: {RecCount} oneri, {WarnCount} uyari",
                result.Recommendations?.Length ?? 0, result.Warnings?.Length ?? 0);

            return new DailySalesAdvice(
                TopRecommendations: result.Recommendations
                    ?.Select(r => new ProductRecommendation(r.ProductName, r.Platform, r.SuggestedPrice, r.Reason))
                    .ToList()
                    ?? new List<ProductRecommendation>(),
                Warnings: result.Warnings
                    ?.Select(w => new ProductWarning(w.ProductName, w.Reason, w.Action))
                    .ToList()
                    ?? new List<ProductWarning>(),
                PlatformHealth: result.PlatformHealth
                    ?.Select(p => new PlatformHealth(p.Platform, p.MarginTrend, p.AvgMargin, p.Suggestion))
                    .ToList()
                    ?? new List<PlatformHealth>(),
                GeneratedAt: DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "[Advisory V2] MESA OS erisim sorunu, kural tabanlı tavsiye uretiliyor");

            var analysisData = await CollectAnalysisDataAsync(tenantId, ct);
            return GenerateRuleBasedAdvice(analysisData);
        }
    }

    private async Task<AnalysisData> CollectAnalysisDataAsync(Guid tenantId, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var thirtyDaysAgo = today.AddDays(-30);

        // Son 30 gunluk ProfitReport'lari al
        var reports = new List<Domain.Accounting.Entities.ProfitReport>();
        for (var day = thirtyDaysAgo; day <= today; day = day.AddDays(1))
        {
            var period = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dayReports = await _profitReportRepository.GetByPeriodAsync(tenantId, period, ct: ct);
            reports.AddRange(dayReports);
        }

        // Son 30 gunluk CommissionRecord'lari platform bazinda al
        var platforms = new[] { "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon" };
        var commissions = new List<Domain.Accounting.Entities.CommissionRecord>();
        foreach (var platform in platforms)
        {
            var platformCommissions = await _commissionRepository
                .GetByPlatformAsync(tenantId, platform, thirtyDaysAgo, today, ct);
            commissions.AddRange(platformCommissions);
        }

        // Urun sayisi
        var productCount = await _productRepository.GetCountAsync();

        return new AnalysisData(reports, commissions, productCount);
    }

    /// <summary>
    /// MESA AI erisim sorunu oldugunda kural tabanlı basit tavsiye uretir.
    /// Negatif marj → "fiyat artir veya platformdan cek"
    /// Dusuk marj → "kampanyayi sonlandir"
    /// Yuksek marj → "bestseller, stok kontrol et"
    /// </summary>
    private static DailySalesAdvice GenerateRuleBasedAdvice(AnalysisData data)
    {
        var recommendations = new List<ProductRecommendation>();
        var warnings = new List<ProductWarning>();
        var platformHealthList = new List<PlatformHealth>();

        // Platform bazinda marj hesapla
        var platformGroups = data.Reports
            .Where(r => r.Platform != null)
            .GroupBy(r => r.Platform!)
            .ToList();

        foreach (var group in platformGroups)
        {
            var platform = group.Key;
            var totalRevenue = group.Sum(r => r.TotalRevenue);
            var totalCommission = group.Sum(r => r.TotalCommission);
            var totalNetProfit = group.Sum(r => r.NetProfit);
            var avgMargin = totalRevenue > 0
                ? (totalNetProfit / totalRevenue) * 100m
                : 0m;

            string marginTrend;
            string suggestion;

            if (avgMargin < 0)
            {
                marginTrend = "Negatif";
                suggestion = $"{platform} platformunda negatif marj — fiyat artisi veya urun cekme onerilir.";

                warnings.Add(new ProductWarning(
                    $"{platform} Genel",
                    $"Son 30 gun ortalama marj: %{avgMargin:F1}",
                    "Fiyat artir veya platformdan cek"));
            }
            else if (avgMargin < 5)
            {
                marginTrend = "Dusuk";
                suggestion = $"{platform} marji %{avgMargin:F1} — komisyon ve kargo giderlerini inceleyin.";
            }
            else if (avgMargin < 15)
            {
                marginTrend = "Normal";
                suggestion = $"{platform} marji saglıklı (%{avgMargin:F1}) — mevcut fiyatlandirmayi koruyun.";
            }
            else
            {
                marginTrend = "Yuksek";
                suggestion = $"{platform} marji yuksek (%{avgMargin:F1}) — satis hacmini artirma firsati.";

                recommendations.Add(new ProductRecommendation(
                    $"{platform} Bestseller",
                    platform,
                    0m,
                    $"Yuksek marjlı platform — reklam butcesi artirarak satis hacmi yukseltilmelidir."));
            }

            platformHealthList.Add(new PlatformHealth(platform, marginTrend, avgMargin, suggestion));
        }

        // Komisyon orani yuksek olan kategoriler icin uyari
        var highCommissionCategories = data.Commissions
            .Where(c => c.CommissionRate > 0.20m)
            .GroupBy(c => new { c.Platform, c.Category })
            .Take(5)
            .ToList();

        foreach (var group in highCommissionCategories)
        {
            var avgRate = group.Average(c => c.CommissionRate);
            warnings.Add(new ProductWarning(
                $"{group.Key.Category ?? "Genel"} ({group.Key.Platform})",
                $"Komisyon orani cok yuksek: %{avgRate * 100:F1}",
                "Kategori degisikligi veya fiyat ayarlamasi yapilmali"));
        }

        // Genel oneriler
        if (data.Reports.Count == 0)
        {
            recommendations.Add(new ProductRecommendation(
                "Genel",
                "Tum Platformlar",
                0m,
                "Son 30 gune ait satis verisi bulunamadi — veri kaynaklarini kontrol edin."));
        }

        return new DailySalesAdvice(
            TopRecommendations: recommendations,
            Warnings: warnings,
            PlatformHealth: platformHealthList,
            GeneratedAt: DateTime.UtcNow);
    }

    private record AnalysisData(
        IReadOnlyList<Domain.Accounting.Entities.ProfitReport> Reports,
        IReadOnlyList<Domain.Accounting.Entities.CommissionRecord> Commissions,
        int ProductCount);
}

/// <summary>
/// MESA OS /api/v1/accounting/advisory/sales yanit modeli.
/// </summary>
internal record MesaSalesAdviceResponse
{
    public MesaProductRecommendation[]? Recommendations { get; init; }
    public MesaProductWarning[]? Warnings { get; init; }
    public MesaPlatformHealth[]? PlatformHealth { get; init; }
}

internal record MesaProductRecommendation
{
    public string ProductName { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public decimal SuggestedPrice { get; init; }
    public string Reason { get; init; } = string.Empty;
}

internal record MesaProductWarning
{
    public string ProductName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
}

internal record MesaPlatformHealth
{
    public string Platform { get; init; } = string.Empty;
    public string MarginTrend { get; init; } = string.Empty;
    public decimal AvgMargin { get; init; }
    public string Suggestion { get; init; } = string.Empty;
}
