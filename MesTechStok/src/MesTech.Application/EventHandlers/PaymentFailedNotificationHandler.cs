using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Ödeme başarısız olduğunda bildirim kaydı oluşturur.
/// PaymentFailedEvent → NotificationLog (ödeme hatası uyarısı).
/// </summary>
public interface IPaymentFailedNotificationHandler
{
    Task HandleAsync(
        Guid tenantId, Guid subscriptionId, string? errorMessage,
        int failureCount, CancellationToken ct);
}

public sealed class PaymentFailedNotificationHandler : IPaymentFailedNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PaymentFailedNotificationHandler> _logger;

    public PaymentFailedNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<PaymentFailedNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid tenantId, Guid subscriptionId, string? errorMessage,
        int failureCount, CancellationToken ct)
    {
        _logger.LogCritical(
            "PaymentFailed → ödeme hatası bildirimi. SubscriptionId={SubId}, FailureCount={Count}, Error={Error}",
            subscriptionId, failureCount, errorMessage);

        var content = $"ÖDEME HATASI: Abonelik ödemesi başarısız (deneme #{failureCount}). " +
                      $"Hata: {errorMessage ?? "Bilinmiyor"}";

        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Email,
            "admin",
            "PaymentFailed",
            content);

        await _notificationRepo.AddAsync(notification, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
