using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// Komisyon hesaplama worker — yeni siparisler icin komisyon kaydı olusturur.
/// Event-triggered olarak tek siparis icin cagrilabilir,
/// ayni zamanda recurring job olarak dun gelen siparisleri tarar.
/// Her 15 dakikada bir calisir.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class CommissionCalculatorWorker : IAccountingJob
{
    public string JobId => "accounting-commission-calculator";
    public string CronExpression => "*/15 * * * *"; // Her 15 dakika

    private readonly ICommissionRecordRepository _commissionRepository;
    private readonly ICommissionCalculationService _calculationService;
    private readonly IOrderRepository _orderRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CommissionCalculatorWorker> _logger;

    public CommissionCalculatorWorker(
        ICommissionRecordRepository commissionRepository,
        ICommissionCalculationService calculationService,
        IOrderRepository orderRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ILogger<CommissionCalculatorWorker> logger)
    {
        _commissionRepository = commissionRepository;
        _calculationService = calculationService;
        _orderRepository = orderRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Komisyon hesaplama basliyor...", JobId);

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var processedCount = 0;

        try
        {
            // Son 24 saatin siparislerini al
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;
            var recentOrders = await _orderRepository.GetByDateRangeAsync(from, to).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] {Count} siparis bulundu ({From:g} - {To:g})",
                JobId, recentOrders.Count, from, to);

            foreach (var order in recentOrders)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // Mevcut komisyon kaydi var mi kontrol et
                    var existingRecords = await _commissionRepository
                        .GetByPlatformAsync(tenantId, order.SourcePlatform?.ToString() ?? "Unknown", from, to, ct).ConfigureAwait(false);

                    var alreadyProcessed = existingRecords
                        .Any(r => r.OrderId == order.OrderNumber);

                    if (alreadyProcessed)
                    {
                        continue;
                    }

                    var platform = order.SourcePlatform?.ToString() ?? "Unknown";
                    var grossAmount = order.TotalAmount;

                    var commission = _calculationService.CalculateCommission(
                        platform, null, grossAmount);

                    var rate = _calculationService.GetDefaultRate(platform);

                    var record = Domain.Accounting.Entities.CommissionRecord.Create(
                        tenantId: tenantId,
                        platform: platform,
                        grossAmount: grossAmount,
                        commissionRate: rate,
                        commissionAmount: commission,
                        serviceFee: 0m,
                        orderId: order.OrderNumber,
                        category: null);

                    await _commissionRepository.AddAsync(record, ct).ConfigureAwait(false);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[{JobId}] Siparis {OrderId} komisyon hesaplama hatasi, atlaniyor",
                        JobId, order.Id);
                }
            }

            if (processedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "[{JobId}] Komisyon hesaplama tamamlandi — {ProcessedCount} siparis islendi",
                JobId, processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Komisyon hesaplama HATA", JobId);
            throw;
        }
    }

    /// <summary>
    /// Tek bir siparis icin komisyon hesaplar (event-triggered).
    /// </summary>
    public async Task CalculateForOrderAsync(
        string orderId,
        string platform,
        string? category,
        decimal grossAmount,
        CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        _logger.LogInformation(
            "[{JobId}] Tek siparis komisyon hesaplama: OrderId={OrderId}, Platform={Platform}",
            JobId, orderId, platform);

        var commission = _calculationService.CalculateCommission(platform, category, grossAmount);
        var rate = _calculationService.GetDefaultRate(platform);

        var record = Domain.Accounting.Entities.CommissionRecord.Create(
            tenantId: tenantId,
            platform: platform,
            grossAmount: grossAmount,
            commissionRate: rate,
            commissionAmount: commission,
            serviceFee: 0m,
            orderId: orderId,
            category: category);

        await _commissionRepository.AddAsync(record, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[{JobId}] Komisyon kaydedildi: OrderId={OrderId}, Commission={Commission:F2}",
            JobId, orderId, commission);
    }
}
