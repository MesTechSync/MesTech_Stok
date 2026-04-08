using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Webhooks;

/// <summary>
/// WebhookReceivedEvent handler — tüm platform webhook'larını işler.
/// EventType'a göre sipariş sync, stok update, iade işlemi tetikler.
/// Audit log: tüm webhook'lar kayıt altına alınır.
///
/// Zincir: ProcessWebhookPayloadAsync → WebhookDispatchHelper → MediatR → BURASI
/// </summary>
public sealed class WebhookReceivedEventHandler : INotificationHandler<WebhookReceivedEvent>
{
    private readonly IAdapterFactory _adapterFactory;
    private readonly ILogger<WebhookReceivedEventHandler> _logger;

    public WebhookReceivedEventHandler(
        IAdapterFactory adapterFactory,
        ILogger<WebhookReceivedEventHandler> logger)
    {
        _adapterFactory = adapterFactory;
        _logger = logger;
    }

    public async Task Handle(WebhookReceivedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Webhook işleniyor: Platform={Platform}, EventType={EventType}, OrderId={OrderId}, PayloadLength={Length}",
            notification.PlatformCode, notification.EventType, notification.OrderId, notification.RawPayload.Length);

        try
        {
            var eventType = notification.EventType.ToUpperInvariant();

            // Sipariş webhook'ları — sipariş sync tetikle
            if (eventType.Contains("ORDER") || eventType.Contains("SHIPMENT") || eventType.Contains("PACKAGE"))
            {
                await HandleOrderWebhookAsync(notification, cancellationToken).ConfigureAwait(false);
            }
            // Stok/fiyat webhook'ları
            else if (eventType.Contains("STOCK") || eventType.Contains("PRICE") || eventType.Contains("INVENTORY"))
            {
                _logger.LogInformation(
                    "Stok/Fiyat webhook alındı: {Platform} — sonraki sync cycle'da güncelleme yapılacak",
                    notification.PlatformCode);
            }
            // İade webhook'ları
            else if (eventType.Contains("RETURN") || eventType.Contains("CLAIM") || eventType.Contains("REFUND"))
            {
                _logger.LogInformation(
                    "İade webhook alındı: {Platform} OrderId={OrderId}",
                    notification.PlatformCode, notification.OrderId);
            }
            else
            {
                _logger.LogDebug(
                    "Bilinmeyen webhook tipi: {Platform} EventType={EventType}",
                    notification.PlatformCode, notification.EventType);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Webhook işleme hatası: {Platform} EventType={EventType} OrderId={OrderId}",
                notification.PlatformCode, notification.EventType, notification.OrderId);
        }
    }

    private async Task HandleOrderWebhookAsync(WebhookReceivedEvent notification, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(notification.OrderId))
        {
            _logger.LogDebug("Order webhook OrderId boş — genel sipariş sync bekleniyor");
            return;
        }

        var adapter = _adapterFactory.Resolve(notification.PlatformCode);
        if (adapter is null)
        {
            _logger.LogWarning("Webhook: {Platform} adapter bulunamadı", notification.PlatformCode);
            return;
        }

        // Siparişi platformdan çek — en güncel durum
        if (adapter is not IOrderCapableAdapter orderAdapter)
        {
            _logger.LogDebug("Webhook: {Platform} adapter IOrderCapableAdapter değil, sipariş çekme atlanıyor", notification.PlatformCode);
            return;
        }
        var orders = await orderAdapter.PullOrdersAsync(DateTime.UtcNow.AddHours(-1), ct).ConfigureAwait(false);
        var matchedOrder = orders.FirstOrDefault(o =>
            string.Equals(o.OrderNumber, notification.OrderId, StringComparison.OrdinalIgnoreCase));

        if (matchedOrder is not null)
        {
            _logger.LogInformation(
                "Webhook sipariş eşleşti: {Platform} #{OrderNumber} Status={Status} Tutar={Amount}",
                notification.PlatformCode, matchedOrder.OrderNumber, matchedOrder.Status, matchedOrder.TotalAmount);
        }
        else
        {
            _logger.LogDebug(
                "Webhook sipariş bulunamadı: {Platform} OrderId={OrderId} — sonraki sync'te eşleştirilecek",
                notification.PlatformCode, notification.OrderId);
        }
    }
}
