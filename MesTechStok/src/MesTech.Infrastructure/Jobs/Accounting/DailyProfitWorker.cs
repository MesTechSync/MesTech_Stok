using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// Gunluk kar/zarar hesaplama worker.
/// Her gun 23:59'da calisir ve ProfitReport entity'si olusturur.
/// </summary>
public class DailyProfitWorker : IAccountingJob
{
    public string JobId => "accounting-daily-profit";
    public string CronExpression => "59 23 * * *"; // Her gun 23:59

    private readonly IProfitCalculationService _profitCalculationService;
    private readonly IProfitReportRepository _profitReportRepository;
    private readonly ICommissionRecordRepository _commissionRepository;
    private readonly ICargoExpenseRepository _cargoExpenseRepository;
    private readonly ITaxRecordRepository _taxRecordRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DailyProfitWorker> _logger;

    public DailyProfitWorker(
        IProfitCalculationService profitCalculationService,
        IProfitReportRepository profitReportRepository,
        ICommissionRecordRepository commissionRepository,
        ICargoExpenseRepository cargoExpenseRepository,
        ITaxRecordRepository taxRecordRepository,
        IOrderRepository orderRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ILogger<DailyProfitWorker> logger)
    {
        _profitCalculationService = profitCalculationService;
        _profitReportRepository = profitReportRepository;
        _commissionRepository = commissionRepository;
        _cargoExpenseRepository = cargoExpenseRepository;
        _taxRecordRepository = taxRecordRepository;
        _orderRepository = orderRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Gunluk kar/zarar hesaplama basliyor...", JobId);

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var period = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        try
        {
            // Mevcut rapor var mi kontrol et (idempotency)
            var existingReports = await _profitReportRepository
                .GetByPeriodAsync(tenantId, period, ct: ct);

            if (existingReports.Count > 0)
            {
                _logger.LogInformation(
                    "[{JobId}] {Period} donemi icin rapor zaten mevcut, atlaniyor",
                    JobId, period);
                return;
            }

            // Bugunku siparisleri al
            var orders = await _orderRepository.GetByDateRangeAsync(today, tomorrow);

            if (orders.Count == 0)
            {
                _logger.LogInformation(
                    "[{JobId}] {Period} donemi icin siparis yok, bos rapor olusturuluyor",
                    JobId, period);
            }

            // Gelir hesapla (siparis toplami)
            var totalRevenue = orders.Sum(o => o.TotalAmount);

            // Maliyet tahmini — SubTotal ile TotalAmount farki + biliniyorsa direkt maliyet
            // Gercek maliyet icin urun maliyeti gerekir, burada siparis bazinda SubTotal kullanilir
            var totalCost = orders.Sum(o => o.SubTotal);

            // Komisyon toplami
            var totalCommission = await _commissionRepository
                .GetTotalCommissionAsync(tenantId, today, tomorrow, ct);

            // Kargo gideri
            var totalCargo = await _cargoExpenseRepository
                .GetTotalCostAsync(tenantId, today, tomorrow, ct);

            // Vergi toplami
            var totalTax = await _taxRecordRepository
                .GetTotalTaxByPeriodAsync(tenantId, period, ct);

            // Kar hesapla
            var netProfit = _profitCalculationService.CalculateNetProfit(
                totalRevenue, totalCost, totalCommission, totalCargo, totalTax);

            var profitMargin = _profitCalculationService.CalculateProfitMargin(
                totalRevenue, netProfit);

            // ProfitReport olustur
            var report = ProfitReport.Create(
                tenantId: tenantId,
                reportDate: today,
                period: period,
                totalRevenue: totalRevenue,
                totalCost: totalCost,
                totalCommission: totalCommission,
                totalCargo: totalCargo,
                totalTax: totalTax);

            await _profitReportRepository.AddAsync(report, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[{JobId}] Gunluk kar/zarar raporu olusturuldu — Period: {Period}, " +
                "Gelir: {Revenue:F2}, Maliyet: {Cost:F2}, Komisyon: {Commission:F2}, " +
                "Kargo: {Cargo:F2}, Vergi: {Tax:F2}, Net: {Net:F2}, Marj: {Margin:F2}%",
                JobId, period, totalRevenue, totalCost, totalCommission,
                totalCargo, totalTax, netProfit, profitMargin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Gunluk kar/zarar hesaplama HATA", JobId);
            throw;
        }
    }
}
