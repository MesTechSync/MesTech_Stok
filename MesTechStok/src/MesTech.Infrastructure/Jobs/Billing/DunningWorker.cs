using Hangfire;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs.Billing;

/// <summary>
/// Dunning (tahsilat takip) worker'i — PastDue aboneliklere kademeli islem uygular.
/// Gun 3: Uyari bildirimi gonder.
/// Gun 7: Odemeyi tekrar dene, basarisizsa premium ozellikleri devre disi birak.
/// Gun 14: Son deneme, basarisizsa aboneligi iptal et.
/// Her gun 04:00'te calisir.
/// </summary>
[AutomaticRetry(Attempts = 0)]
public sealed class DunningWorker
{
    public const string JobId = "dunning-escalation";

    private readonly ITenantSubscriptionRepository _subscriptionRepository;
    private readonly IDunningLogRepository _dunningLogRepository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DunningWorker> _logger;

    public DunningWorker(
        ITenantSubscriptionRepository subscriptionRepository,
        IDunningLogRepository dunningLogRepository,
        IPaymentProvider paymentProvider,
        INotificationService notificationService,
        IUnitOfWork unitOfWork,
        ILogger<DunningWorker> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _dunningLogRepository = dunningLogRepository;
        _paymentProvider = paymentProvider;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessDunningAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Dunning escalation islemi basliyor...", JobId);

        var pastDueSubscriptions = await _subscriptionRepository
            .GetByStatusAsync(SubscriptionStatus.PastDue, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "[{JobId}] {Count} adet PastDue abonelik bulundu",
            JobId, pastDueSubscriptions.Count);

        var processedCount = 0;

        foreach (var subscription in pastDueSubscriptions)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var daysSincePastDue = GetDaysSincePastDue(subscription);

                _logger.LogDebug(
                    "[{JobId}] SubscriptionId={SubscriptionId}, DaysSincePastDue={Days}",
                    JobId, subscription.Id, daysSincePastDue);

                switch (daysSincePastDue)
                {
                    case >= 14:
                        await HandleDay14CancelAsync(subscription, ct).ConfigureAwait(false);
                        processedCount++;
                        break;

                    case >= 7:
                        await HandleDay7RetryAsync(subscription, ct).ConfigureAwait(false);
                        processedCount++;
                        break;

                    case >= 3:
                        await HandleDay3WarningAsync(subscription, ct).ConfigureAwait(false);
                        processedCount++;
                        break;

                    default:
                        _logger.LogDebug(
                            "[{JobId}] SubscriptionId={SubscriptionId} — henuz escalation gerekmez ({Days} gun)",
                            JobId, subscription.Id, daysSincePastDue);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{JobId}] Dunning islemi hatasi: SubscriptionId={SubscriptionId}",
                    JobId, subscription.Id);
            }
        }

        _logger.LogInformation(
            "[{JobId}] Dunning escalation tamamlandi — {Count} abonelik islendi",
            JobId, processedCount);
    }

    /// <summary>Gun 3: Uyari bildirimi gonder.</summary>
    private async Task HandleDay3WarningAsync(TenantSubscription subscription, CancellationToken ct)
    {
        _logger.LogInformation(
            "[{JobId}] Gun 3 uyari: SubscriptionId={SubscriptionId}",
            JobId, subscription.Id);

        await _notificationService.NotifyAsync(
            "Odeme Basarisiz — Abonelik Uyarisi",
            $"Aboneliginiz icin odeme alinamadi. Lutfen odeme yontintemizi guncelleyin. Abonelik ID: {subscription.Id}",
            NotificationLevel.Warning,
            ct).ConfigureAwait(false);

        var attemptCount = await _dunningLogRepository
            .GetAttemptCountAsync(subscription.Id, ct).ConfigureAwait(false);

        var log = DunningLog.Create(
            tenantId: subscription.TenantId,
            tenantSubscriptionId: subscription.Id,
            attemptNumber: attemptCount + 1,
            action: DunningAction.Warning,
            success: true);

        await _dunningLogRepository.AddAsync(log, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Gun 7: Odemeyi tekrar dene, basarisizsa premium ozellikleri devre disi birak.</summary>
    private async Task HandleDay7RetryAsync(TenantSubscription subscription, CancellationToken ct)
    {
        _logger.LogInformation(
            "[{JobId}] Gun 7 yeniden deneme: SubscriptionId={SubscriptionId}",
            JobId, subscription.Id);

        var amount = GetRenewalAmount(subscription);
        var success = false;
        string? errorMessage = null;

        try
        {
            var paymentResult = await _paymentProvider.ProcessPaymentAsync(
                new PaymentRequest(
                    OrderId: subscription.Id,
                    Amount: amount,
                    Currency: subscription.Plan?.CurrencyCode ?? "TRY",
                    CardToken: null,
                    ReturnUrl: string.Empty,
                    CustomerIp: "0.0.0.0"),
                ct).ConfigureAwait(false);

            success = paymentResult.Success;
            errorMessage = paymentResult.ErrorMessage;

            if (success)
            {
                subscription.Renew();
                await _subscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "[{JobId}] Gun 7 yeniden deneme BASARILI: SubscriptionId={SubscriptionId}",
                    JobId, subscription.Id);
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogWarning(ex,
                "[{JobId}] Gun 7 odeme denemesi hatasi: SubscriptionId={SubscriptionId}",
                JobId, subscription.Id);
        }

        if (!success)
        {
            // Premium ozellikleri devre disi birak — bildirim gonder
            await _notificationService.NotifyAsync(
                "Premium Ozellikler Askiya Alindi",
                $"Ikinci odeme denemesi de basarisiz oldu. Premium ozellikler gecici olarak devre disi birakildi. Abonelik ID: {subscription.Id}",
                NotificationLevel.Error,
                ct).ConfigureAwait(false);
        }

        var attemptCount = await _dunningLogRepository
            .GetAttemptCountAsync(subscription.Id, ct).ConfigureAwait(false);

        var log = DunningLog.Create(
            tenantId: subscription.TenantId,
            tenantSubscriptionId: subscription.Id,
            attemptNumber: attemptCount + 1,
            action: success ? DunningAction.Retry : DunningAction.Suspend,
            success: success,
            errorMessage: errorMessage);

        await _dunningLogRepository.AddAsync(log, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Gun 14: Son deneme, basarisizsa aboneligi iptal et.</summary>
    private async Task HandleDay14CancelAsync(TenantSubscription subscription, CancellationToken ct)
    {
        _logger.LogInformation(
            "[{JobId}] Gun 14 son deneme: SubscriptionId={SubscriptionId}",
            JobId, subscription.Id);

        var amount = GetRenewalAmount(subscription);
        var success = false;
        string? errorMessage = null;

        try
        {
            var paymentResult = await _paymentProvider.ProcessPaymentAsync(
                new PaymentRequest(
                    OrderId: subscription.Id,
                    Amount: amount,
                    Currency: subscription.Plan?.CurrencyCode ?? "TRY",
                    CardToken: null,
                    ReturnUrl: string.Empty,
                    CustomerIp: "0.0.0.0"),
                ct).ConfigureAwait(false);

            success = paymentResult.Success;
            errorMessage = paymentResult.ErrorMessage;

            if (success)
            {
                subscription.Renew();
                await _subscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "[{JobId}] Gun 14 son deneme BASARILI: SubscriptionId={SubscriptionId}",
                    JobId, subscription.Id);
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogWarning(ex,
                "[{JobId}] Gun 14 odeme denemesi hatasi: SubscriptionId={SubscriptionId}",
                JobId, subscription.Id);
        }

        if (!success)
        {
            subscription.Cancel("Dunning: 14 gun sonra odeme alinamadi — otomatik iptal");
            await _subscriptionRepository.UpdateAsync(subscription, ct).ConfigureAwait(false);

            await _notificationService.NotifyAsync(
                "Abonelik Iptal Edildi",
                $"14 gun boyunca odeme alinamadigi icin aboneliginiz iptal edildi. Yeniden aktiflestirmek icin odeme yapiniz. Abonelik ID: {subscription.Id}",
                NotificationLevel.Critical,
                ct).ConfigureAwait(false);

            _logger.LogWarning(
                "[{JobId}] Abonelik iptal edildi: SubscriptionId={SubscriptionId}",
                JobId, subscription.Id);
        }

        var attemptCount = await _dunningLogRepository
            .GetAttemptCountAsync(subscription.Id, ct).ConfigureAwait(false);

        var log = DunningLog.Create(
            tenantId: subscription.TenantId,
            tenantSubscriptionId: subscription.Id,
            attemptNumber: attemptCount + 1,
            action: success ? DunningAction.Retry : DunningAction.Cancel,
            success: success,
            errorMessage: errorMessage);

        await _dunningLogRepository.AddAsync(log, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static int GetDaysSincePastDue(TenantSubscription subscription)
    {
        // NextBillingDate zamani gecmis oldugu icin, aradaki farki hesapla
        if (!subscription.NextBillingDate.HasValue)
            return 0;

        var elapsed = DateTime.UtcNow.Date - subscription.NextBillingDate.Value.Date;
        return Math.Max(0, (int)elapsed.TotalDays);
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
