using MesTech.Application.DTOs;
using MesTech.Application.Features.Accounting.Commands.ImportSettlement;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// Platform settlement verilerini periyodik olarak ceken Hangfire worker.
/// Yapilandirilan platformlardan ISettlementCapableAdapter uzerinden settlement verisini alir,
/// ImportSettlementCommand ile DB'ye kaydeder.
/// Her gun 03:30'da calisir.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class SettlementSyncWorker : IAccountingJob
{
    public string JobId => "accounting-settlement-sync";
    public string CronExpression => "30 3 * * *"; // Her gun 03:30

    private readonly IAdapterFactory _adapterFactory;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMediator _mediator;
    private readonly ILogger<SettlementSyncWorker> _logger;

    public SettlementSyncWorker(
        IAdapterFactory adapterFactory,
        ITenantProvider tenantProvider,
        IMediator mediator,
        ILogger<SettlementSyncWorker> logger)
    {
        _adapterFactory = adapterFactory;
        _tenantProvider = tenantProvider;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Settlement sync basliyor...", JobId);

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var today = DateTime.UtcNow.Date;
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var totalSettlements = 0;

        var settlementAdapters = _adapterFactory.GetAll()
            .Where(a => a is ISettlementCapableAdapter)
            .ToList();

        foreach (var registeredAdapter in settlementAdapters)
        {
            ct.ThrowIfCancellationRequested();
            var platform = registeredAdapter.PlatformCode;

            try
            {
                var settlementAdapter = registeredAdapter as ISettlementCapableAdapter;
                if (settlementAdapter == null) continue;

                var settlement = await settlementAdapter.GetSettlementAsync(yesterday, today, ct).ConfigureAwait(false);
                if (settlement != null && settlement.Lines.Count > 0)
                {
                    await PersistSettlementAsync(tenantId, platform, settlement, yesterday, today, ct)
                        .ConfigureAwait(false);
                    totalSettlements++;
                    _logger.LogInformation(
                        "[{JobId}] {Platform} settlement cekildi ve kaydedildi: {StartDate:d} - {EndDate:d}, {Lines} satir",
                        JobId, platform, yesterday, today, settlement.Lines.Count);
                }

                var cargoInvoices = await settlementAdapter.GetCargoInvoicesAsync(yesterday, ct).ConfigureAwait(false);
                _logger.LogInformation(
                    "[{JobId}] {Platform} — {CargoCount} kargo faturasi cekildi",
                    JobId, platform, cargoInvoices.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[{JobId}] {Platform} settlement sync HATA", JobId, platform);
                // Devam et — diger platformlari denemeyi birakma
            }
        }

        _logger.LogInformation(
            "[{JobId}] Settlement sync tamamlandi — {Total} platform settlement kaydedildi",
            JobId, totalSettlements);
    }

    private async Task PersistSettlementAsync(
        Guid tenantId, string platform, SettlementDto settlement,
        DateTime periodStart, DateTime periodEnd, CancellationToken ct)
    {
        var lines = settlement.Lines.Select(l => new SettlementLineInput(
            OrderId: l.OrderNumber,
            GrossAmount: l.Amount,
            CommissionAmount: l.CommissionAmount ?? 0m,
            ServiceFee: l.ServiceFee,
            CargoDeduction: l.CargoDeduction,
            RefundDeduction: l.RefundDeduction,
            NetAmount: l.NetAmount != 0 ? l.NetAmount : l.Amount - (l.CommissionAmount ?? 0m),
            VatAmount: l.VatAmount
        )).ToList();

        var command = new ImportSettlementCommand(
            TenantId: tenantId,
            Platform: platform,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            TotalGross: settlement.TotalSales,
            TotalCommission: settlement.TotalCommission,
            TotalNet: settlement.NetAmount,
            Lines: lines);

        await _mediator.Send(command, ct).ConfigureAwait(false);
    }
}
