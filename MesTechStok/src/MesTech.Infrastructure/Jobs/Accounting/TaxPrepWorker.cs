using System.Globalization;
using MassTransit;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.AI.Accounting;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// Aylik KDV taslagi hazirlama worker — her ayin 1'inde saat 06:00'da calisir.
/// Onceki ayin vergi taslagini TaxPrepAgent uzerinden hesaplar ve
/// RabbitMQ uzerinden bot bildirim icin publish eder.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class TaxPrepWorker : IAccountingJob
{
    public string JobId => "accounting-tax-prep";
    public string CronExpression => "0 6 1 * *"; // Her ayin 1'i saat 06:00

    private readonly ITaxPrepAgent _taxPrepAgent;
    private readonly ITenantProvider _tenantProvider;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TaxPrepWorker> _logger;

    public TaxPrepWorker(
        ITaxPrepAgent taxPrepAgent,
        ITenantProvider tenantProvider,
        IPublishEndpoint publishEndpoint,
        ILogger<TaxPrepWorker> logger)
    {
        _taxPrepAgent = taxPrepAgent;
        _tenantProvider = tenantProvider;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Aylik vergi taslagi hazirlaniyor...", JobId);

        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Onceki ayi hesapla
        var now = DateTime.UtcNow;
        var previousMonth = now.AddMonths(-1);
        var year = previousMonth.Year;
        var month = previousMonth.Month;

        try
        {
            var report = await _taxPrepAgent.PrepareMonthlyTaxAsync(tenantId, year, month, ct).ConfigureAwait(false);

            // RabbitMQ uzerinden bot bildirim publish
            await _publishEndpoint.Publish(new FinanceTaxPrepReadyEvent(
                Year: report.Year,
                Month: report.Month,
                TotalSales: report.TotalSales,
                TotalPurchases: report.TotalPurchases,
                CalculatedVAT: report.CalculatedVAT,
                DeductibleVAT: report.DeductibleVAT,
                PayableVAT: report.PayableVAT,
                TotalWithholding: report.TotalWithholding,
                TotalStopaj: report.TotalStopaj,
                Disclaimer: report.Disclaimer,
                TenantId: tenantId,
                OccurredAt: DateTime.UtcNow), ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Vergi taslagi yayinlandi — Donem: {Year}-{Month:D2}, " +
                "Odenecek KDV: {PayableVAT:F2}, Tevkifat: {Withholding:F2}",
                JobId, year, month, report.PayableVAT, report.TotalWithholding);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{JobId}] Aylik vergi taslagi hazirlama HATA — Donem: {Year}-{Month:D2}",
                JobId, year, month);
            throw;
        }
    }
}
