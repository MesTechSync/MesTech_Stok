using System.Globalization;
using MassTransit;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.AI.Accounting;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// Gunluk finansal brifing worker — her gun 08:00'da calisir.
/// Dunkun ProfitReport, komisyon ozeti ve dusuk stok uyarilarini toplar,
/// Advisory V2 satis tavsiyeleri ve platform saglik bilgilerini ekler,
/// ay sonu yaklasiyorsa TaxPrepAgent ile KDV tahmini ekler,
/// FinanceReportDailyEvent publish eder — MESA Bot Gateway WhatsApp/Telegram iletir.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class ScheduledBriefingWorker : IAccountingJob
{
    public string JobId => "accounting-scheduled-briefing";
    public string CronExpression => "0 8 * * *"; // Her gun 08:00

    private readonly IProfitReportRepository _profitReportRepository;
    private readonly ICommissionRecordRepository _commissionRepository;
    private readonly ICargoExpenseRepository _cargoExpenseRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IAdvisoryAgentV2 _advisoryAgentV2;
    private readonly ITaxPrepAgent _taxPrepAgent;
    private readonly ILogger<ScheduledBriefingWorker> _logger;

    public ScheduledBriefingWorker(
        IProfitReportRepository profitReportRepository,
        ICommissionRecordRepository commissionRepository,
        ICargoExpenseRepository cargoExpenseRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ITenantProvider tenantProvider,
        IPublishEndpoint publishEndpoint,
        IAdvisoryAgentV2 advisoryAgentV2,
        ITaxPrepAgent taxPrepAgent,
        ILogger<ScheduledBriefingWorker> logger)
    {
        _profitReportRepository = profitReportRepository;
        _commissionRepository = commissionRepository;
        _cargoExpenseRepository = cargoExpenseRepository;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _tenantProvider = tenantProvider;
        _publishEndpoint = publishEndpoint;
        _advisoryAgentV2 = advisoryAgentV2;
        _taxPrepAgent = taxPrepAgent;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Gunluk finansal brifing hazirlaniyor...", JobId);

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var today = DateTime.UtcNow.Date;
        var period = yesterday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        try
        {
            // 1. Dunkun ProfitReport'u al
            var reports = await _profitReportRepository.GetByPeriodAsync(tenantId, period, ct: ct).ConfigureAwait(false);
            var report = reports.Count > 0 ? reports[0] : null;

            // FIX-DEV6-TUR4: Fetch orders once — reuse for fallback calc and order count
            var yesterdayOrders = await _orderRepository.GetByDateRangeAsync(yesterday, today).ConfigureAwait(false);
            var orderCount = yesterdayOrders.Count;

            decimal totalRevenue = 0m;
            decimal totalCommission = 0m;
            decimal totalCargo = 0m;
            decimal netProfit = 0m;

            if (report != null)
            {
                totalRevenue = report.TotalRevenue;
                totalCommission = report.TotalCommission;
                totalCargo = report.TotalCargo;
                netProfit = report.NetProfit;
            }
            else
            {
                _logger.LogWarning(
                    "[{JobId}] {Period} donemi icin ProfitReport bulunamadi — ham verilerden hesaplaniyor",
                    JobId, period);

                // Fallback: ham verilerden hesapla (yesterdayOrders already fetched above)
                totalRevenue = yesterdayOrders.Sum(o => o.TotalAmount);
                totalCommission = await _commissionRepository.GetTotalCommissionAsync(tenantId, yesterday, today, ct).ConfigureAwait(false);
                totalCargo = await _cargoExpenseRepository.GetTotalCostAsync(tenantId, yesterday, today, ct).ConfigureAwait(false);
                netProfit = totalRevenue - totalCommission - totalCargo;
            }

            // 3. Dusuk stok uyarilari
            var lowStockProducts = await _productRepository.GetLowStockAsync(ct).ConfigureAwait(false);
            var stockAlerts = lowStockProducts
                .Take(10)
                .Select(p => $"{p.Name} (SKU: {p.SKU}) — Stok: {p.Stock}")
                .ToList();

            // 4. Oneriler olustur
            var recommendations = new List<string>();

            if (netProfit < 0)
                recommendations.Add("Negatif kar — gider kalemlerini inceleyin.");

            if (totalCommission > totalRevenue * 0.15m)
                recommendations.Add("Komisyon orani %15 uzerinde — kategori bazli analiz onerilir.");

            if (lowStockProducts.Count > 5)
                recommendations.Add($"{lowStockProducts.Count} urun dusuk stokta — tedarik plani olusturun.");

            if (orderCount == 0)
                recommendations.Add("Dun siparis gelmedi — platform durumunu kontrol edin.");

            // 5. Advisory V2 — "Bugun ne sat" tavsiyeleri (MUH-03)
            await EnrichWithSalesAdviceAsync(tenantId, recommendations, stockAlerts, ct).ConfigureAwait(false);

            // 6. Ay sonu KDV tahmini (MUH-03) — ayin son 5 gununde otomatik hesapla
            await EnrichWithTaxEstimateAsync(tenantId, today, recommendations, ct).ConfigureAwait(false);

            // 7. FinanceReportDailyEvent publish
            await _publishEndpoint.Publish(new FinanceReportDailyEvent(
                Date: yesterday,
                OrderCount: orderCount,
                TotalRevenue: totalRevenue,
                TotalCommission: totalCommission,
                TotalCargo: totalCargo,
                NetProfit: netProfit,
                StockAlerts: stockAlerts,
                Recommendations: recommendations,
                TenantId: tenantId,
                OccurredAt: DateTime.UtcNow), ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Gunluk brifing yayinlandi — Tarih: {Date:d}, " +
                "Siparis: {Orders}, Gelir: {Revenue:F2}, Komisyon: {Commission:F2}, " +
                "Kargo: {Cargo:F2}, Net: {Net:F2}, StokUyari: {Alerts}, Oneri: {Recs}",
                JobId, yesterday, orderCount, totalRevenue, totalCommission,
                totalCargo, netProfit, stockAlerts.Count, recommendations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Gunluk brifing hazirlama HATA", JobId);
            throw;
        }
    }

    /// <summary>
    /// Advisory V2 satis tavsiyeleri ve platform saglik uyarilarini brifingeekler.
    /// Hata durumunda brifing basarisiz olmaz — sadece uyari loglanir.
    /// </summary>
    private async Task EnrichWithSalesAdviceAsync(
        Guid tenantId,
        List<string> recommendations,
        List<string> stockAlerts,
        CancellationToken ct)
    {
        try
        {
            var salesAdvice = await _advisoryAgentV2.GenerateSalesAdviceAsync(tenantId, ct).ConfigureAwait(false);

            // Satis tavsiyeleri
            foreach (var rec in salesAdvice.TopRecommendations.Take(5))
            {
                var priceInfo = rec.SuggestedPrice > 0
                    ? $" (onerilen fiyat: {rec.SuggestedPrice:N2} TL)"
                    : string.Empty;
                recommendations.Add(
                    $"[SATIS] {rec.ProductName} ({rec.Platform}){priceInfo} — {rec.Reason}");
            }

            // Urun uyarilari
            foreach (var warn in salesAdvice.Warnings.Take(5))
            {
                stockAlerts.Add(
                    $"[UYARI] {warn.ProductName}: {warn.Reason} — Aksiyon: {warn.Action}");
            }

            // Platform saglik uyarilari
            foreach (var health in salesAdvice.PlatformHealth)
            {
                if (health.MarginTrend is "Negatif" or "Dusuk")
                {
                    recommendations.Add(
                        $"[PLATFORM] {health.Platform}: Marj %{health.AvgMargin:F1} ({health.MarginTrend}) — {health.Suggestion}");
                }
            }

            _logger.LogInformation(
                "[{JobId}] Advisory V2 verileri brifingeeklendi: " +
                "{RecCount} oneri, {WarnCount} uyari, {PlatformCount} platform",
                JobId, salesAdvice.TopRecommendations.Count,
                salesAdvice.Warnings.Count, salesAdvice.PlatformHealth.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{JobId}] Advisory V2 verileri alinamadi — brifing Advisory V2'siz devam ediyor",
                JobId);
        }
    }

    /// <summary>
    /// Ayin son 5 gununde mevcut ay icin KDV tahmini hesaplar ve brifinge ekler.
    /// </summary>
    private async Task EnrichWithTaxEstimateAsync(
        Guid tenantId,
        DateTime today,
        List<string> recommendations,
        CancellationToken ct)
    {
        // Sadece ayin son 5 gununde KDV tahmini ekle
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        if (today.Day < daysInMonth - 4)
            return;

        try
        {
            var taxReport = await _taxPrepAgent.PrepareMonthlyTaxAsync(
                tenantId, today.Year, today.Month, ct).ConfigureAwait(false);

            recommendations.Add(
                $"[KDV] {today.Year}-{today.Month:D2} ay sonu KDV tahmini — " +
                $"Hesaplanan: {taxReport.CalculatedVAT:N2} TL, " +
                $"Indirilecek: {taxReport.DeductibleVAT:N2} TL, " +
                $"Odenecek: {taxReport.PayableVAT:N2} TL");

            if (taxReport.TotalWithholding > 0)
            {
                recommendations.Add(
                    $"[KDV] Tevkifat toplami: {taxReport.TotalWithholding:N2} TL");
            }

            _logger.LogInformation(
                "[{JobId}] KDV tahmini brifingeeklendi — Odenecek: {PayableVAT:F2}",
                JobId, taxReport.PayableVAT);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[{JobId}] KDV tahmini alinamadi — brifing KDV'siz devam ediyor", JobId);
        }
    }
}
