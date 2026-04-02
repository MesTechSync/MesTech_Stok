using Hangfire;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs.Billing;

/// <summary>
/// Gunluk abonelik yenileme worker'i — vadesi gelen abonelikleri otomatik odeme ile yeniler.
/// Basarisiz odemeler PastDue'ya dusurulur ve DunningLog kaydedilir.
/// Her gun 03:00'te calisir.
/// </summary>
[AutomaticRetry(Attempts = 2)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class SubscriptionRenewalWorker
{
    public const string JobId = "subscription-renewal";
    private const string ServerInitiatedIp = "127.0.0.1";

    private readonly ITenantSubscriptionRepository _subscriptionRepository;
    private readonly IBillingInvoiceRepository _invoiceRepository;
    private readonly IDunningLogRepository _dunningLogRepository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionRenewalWorker> _logger;

    public SubscriptionRenewalWorker(
        ITenantSubscriptionRepository subscriptionRepository,
        IBillingInvoiceRepository invoiceRepository,
        IDunningLogRepository dunningLogRepository,
        IPaymentProvider paymentProvider,
        IUnitOfWork unitOfWork,
        ILogger<SubscriptionRenewalWorker> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _invoiceRepository = invoiceRepository;
        _dunningLogRepository = dunningLogRepository;
        _paymentProvider = paymentProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessRenewalsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Abonelik yenileme islemi basliyor...", JobId);

        var today = DateTime.UtcNow.Date;
        var dueSubscriptions = await _subscriptionRepository
            .GetDueForRenewalAsync(today, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "[{JobId}] {Count} adet vadesi gelen abonelik bulundu",
            JobId, dueSubscriptions.Count);

        var renewedCount = 0;
        var failedCount = 0;

        foreach (var subscription in dueSubscriptions)
        {
            ct.ThrowIfCancellationRequested();

            // Idempotency guard — aynı gün zaten yenilenmişse atla (G100: çift charge önleme)
            if (subscription.NextBillingDate?.Date > today)
            {
                _logger.LogDebug(
                    "[{JobId}] Abonelik bugün zaten yenilenmiş — atlanıyor: SubscriptionId={SubscriptionId}",
                    JobId, subscription.Id);
                continue;
            }

            try
            {
                var amount = GetRenewalAmount(subscription);

                var paymentResult = await _paymentProvider.ProcessPaymentAsync(
                    new PaymentRequest(
                        OrderId: subscription.Id,
                        Amount: amount,
                        Currency: subscription.Plan?.CurrencyCode ?? "TRY",
                        CardToken: null, // stored payment token — will be resolved by provider
                        ReturnUrl: string.Empty,
                        CustomerIp: ServerInitiatedIp),
                    ct).ConfigureAwait(false);

                if (paymentResult.Success)
                {
                    subscription.Renew();
                    await _subscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);

                    // Fatura olustur
                    var sequence = await _invoiceRepository.GetNextSequenceAsync(ct).ConfigureAwait(false);
                    var invoice = BillingInvoice.Create(
                        tenantId: subscription.TenantId,
                        subscriptionId: subscription.Id,
                        invoiceNumber: BillingInvoice.GenerateInvoiceNumber(sequence),
                        amount: amount,
                        currencyCode: subscription.Plan?.CurrencyCode ?? "TRY");

                    invoice.MarkPaid(paymentResult.TransactionId);
                    await _invoiceRepository.AddAsync(invoice, ct).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                    renewedCount++;

                    _logger.LogInformation(
                        "[{JobId}] Abonelik yenilendi: SubscriptionId={SubscriptionId}, Amount={Amount:F2}",
                        JobId, subscription.Id, amount);
                }
                else
                {
                    await HandlePaymentFailureAsync(subscription, paymentResult.ErrorMessage, ct)
                        .ConfigureAwait(false);
                    failedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobId}] Abonelik yenileme hatasi: SubscriptionId={SubscriptionId}",
                    JobId, subscription.Id);

                await HandlePaymentFailureAsync(subscription, ex.Message, ct)
                    .ConfigureAwait(false);
                failedCount++;
            }
        }

        _logger.LogInformation(
            "[{JobId}] Abonelik yenileme tamamlandi — Yenilenen: {Renewed}, Basarisiz: {Failed}",
            JobId, renewedCount, failedCount);
    }

    private async Task HandlePaymentFailureAsync(
        TenantSubscription subscription,
        string? errorMessage,
        CancellationToken ct)
    {
        subscription.MarkPastDue();
        await _subscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);

        var attemptCount = await _dunningLogRepository
            .GetAttemptCountAsync(subscription.Id, ct).ConfigureAwait(false);

        var dunningLog = DunningLog.Create(
            tenantId: subscription.TenantId,
            tenantSubscriptionId: subscription.Id,
            attemptNumber: attemptCount + 1,
            action: DunningAction.Retry,
            success: false,
            errorMessage: errorMessage);

        await _dunningLogRepository.AddAsync(dunningLog, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogWarning(
            "[{JobId}] Odeme basarisiz — PastDue'ya dusuruldu: SubscriptionId={SubscriptionId}, Error={Error}",
            JobId, subscription.Id, errorMessage);
    }

    private static decimal GetRenewalAmount(TenantSubscription subscription)
    {
        if (subscription.Plan == null)
            return 0m;

        return subscription.Period == BillingPeriod.Annual
            ? subscription.Plan.AnnualPrice
            : subscription.Plan.MonthlyPrice;
    }
}
