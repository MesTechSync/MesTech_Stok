using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Abonelik iptal edildiğinde loglama, temizlik ve bildirim tetikleme.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface ISubscriptionCancelledEventHandler
{
    Task HandleAsync(Guid tenantId, Guid subscriptionId, string? reason, CancellationToken ct);
}

public sealed class SubscriptionCancelledEventHandler : ISubscriptionCancelledEventHandler
{
    private readonly ILogger<SubscriptionCancelledEventHandler> _logger;

    public SubscriptionCancelledEventHandler(ILogger<SubscriptionCancelledEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(Guid tenantId, Guid subscriptionId, string? reason, CancellationToken ct)
    {
        _logger.LogWarning(
            "SubscriptionCancelled → TenantId={TenantId}, SubscriptionId={SubscriptionId}, Reason={Reason}",
            tenantId, subscriptionId, reason ?? "No reason provided");

        // FUTURE: Tenant kaynaklarını temizle (grace period sonrası)
        // FUTURE: Bildirim gönder (Email ile onay)
        // FUTURE: ERP'de abonelik kaydını güncelle

        return Task.CompletedTask;
    }
}
