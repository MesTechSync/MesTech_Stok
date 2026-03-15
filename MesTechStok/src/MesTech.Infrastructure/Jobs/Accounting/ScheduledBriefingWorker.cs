using System.Globalization;
using MassTransit;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// Gunluk finansal brifing worker — her gun 08:00'da calisir.
/// Dunkun ProfitReport, komisyon ozeti ve dusuk stok uyarilarini toplar,
/// FinanceReportDailyEvent publish eder — MESA Bot Gateway WhatsApp/Telegram iletir.
/// </summary>
public class ScheduledBriefingWorker : IAccountingJob
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
    private readonly ILogger<ScheduledBriefingWorker> _logger;

    public ScheduledBriefingWorker(
        IProfitReportRepository profitReportRepository,
        ICommissionRecordRepository commissionRepository,
        ICargoExpenseRepository cargoExpenseRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ITenantProvider tenantProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<ScheduledBriefingWorker> logger)
    {
        _profitReportRepository = profitReportRepository;
        _commissionRepository = commissionRepository;
        _cargoExpenseRepository = cargoExpenseRepository;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _tenantProvider = tenantProvider;
        _publishEndpoint = publishEndpoint;
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
            var reports = await _profitReportRepository.GetByPeriodAsync(tenantId, period, ct: ct);
            var report = reports.Count > 0 ? reports[0] : null;

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

                // Fallback: ham verilerden hesapla
                var orders = await _orderRepository.GetByDateRangeAsync(yesterday, today);
                totalRevenue = orders.Sum(o => o.TotalAmount);
                totalCommission = await _commissionRepository.GetTotalCommissionAsync(tenantId, yesterday, today, ct);
                totalCargo = await _cargoExpenseRepository.GetTotalCostAsync(tenantId, yesterday, today, ct);
                netProfit = totalRevenue - totalCommission - totalCargo;
            }

            // 2. Siparis sayisi
            var yesterdayOrders = await _orderRepository.GetByDateRangeAsync(yesterday, today);
            var orderCount = yesterdayOrders.Count;

            // 3. Dusuk stok uyarilari
            var lowStockProducts = await _productRepository.GetLowStockAsync();
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

            // 5. FinanceReportDailyEvent publish
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
                OccurredAt: DateTime.UtcNow), ct);

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
}
