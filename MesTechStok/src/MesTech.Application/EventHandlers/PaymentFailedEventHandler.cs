using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Ödeme başarısız olduğunda hata loglar ve bildirim/retry tetikler.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IPaymentFailedEventHandler
{
    Task HandleAsync(Guid tenantId, Guid subscriptionId, string? errorMessage, string? errorCode, int failureCount, CancellationToken ct);
}

public class PaymentFailedEventHandler : IPaymentFailedEventHandler
{
    private readonly ILogger<PaymentFailedEventHandler> _logger;

    public PaymentFailedEventHandler(ILogger<PaymentFailedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(
        Guid tenantId,
        Guid subscriptionId,
        string? errorMessage,
        string? errorCode,
        int failureCount,
        CancellationToken ct)
    {
        _logger.LogError(
            "PaymentFailed → TenantId={TenantId}, SubscriptionId={SubscriptionId}, ErrorCode={ErrorCode}, ErrorMessage={ErrorMessage}, FailureCount={FailureCount}",
            tenantId, subscriptionId, errorCode ?? "N/A", errorMessage ?? "N/A", failureCount);

        if (failureCount >= 3)
        {
            _logger.LogCritical(
                "Payment failure threshold reached — SubscriptionId={SubscriptionId}, FailureCount={FailureCount}. Manual intervention required.",
                subscriptionId, failureCount);
        }

        // FUTURE: Bildirim gönder (Email/Telegram)
        // FUTURE: Retry logic (exponential backoff)

        return Task.CompletedTask;
    }
}
