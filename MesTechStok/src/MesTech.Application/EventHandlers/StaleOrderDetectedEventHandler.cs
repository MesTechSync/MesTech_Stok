using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// 48 saat geçmiş ama hâlâ gönderilmemiş sipariş tespit edildiğinde bildirim tetikler.
/// Notification trigger handler — satıcıya uyarı gönderilir.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IStaleOrderEventHandler
{
    Task HandleAsync(Guid orderId, string orderNumber, PlatformType? platform, TimeSpan elapsedSince, Guid tenantId, CancellationToken ct);
}

public sealed class StaleOrderDetectedEventHandler : IStaleOrderEventHandler
{
    private readonly ILogger<StaleOrderDetectedEventHandler> _logger;

    public StaleOrderDetectedEventHandler(ILogger<StaleOrderDetectedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(
        Guid orderId,
        string orderNumber,
        PlatformType? platform,
        TimeSpan elapsedSince,
        Guid tenantId,
        CancellationToken ct)
    {
        _logger.LogWarning(
            "StaleOrderDetected → OrderNumber={OrderNumber}, Platform={Platform}, ElapsedSince={ElapsedHours:F1}h, OrderId={OrderId}, TenantId={TenantId}",
            orderNumber, platform?.ToString() ?? "Unknown", elapsedSince.TotalHours, orderId, tenantId);

        // FUTURE: Satıcıya bildirim gönder (Telegram/Email)
        // FUTURE: Dashboard'da stale order uyarısı göster

        return Task.CompletedTask;
    }
}
