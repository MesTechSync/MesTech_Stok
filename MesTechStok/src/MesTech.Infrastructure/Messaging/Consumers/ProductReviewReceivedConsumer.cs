using MassTransit;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Consumers;

/// <summary>
/// Cevapsiz urun degerlendirmesi geldiginde bildirim gonderen consumer.
/// Trendyol ReviewSyncJob tarafindan publish edilen event'leri tuketir.
/// </summary>
public sealed class ProductReviewReceivedConsumer : IConsumer<ProductReviewReceivedIntegrationEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProductReviewReceivedConsumer> _logger;

    public ProductReviewReceivedConsumer(
        INotificationService notificationService,
        ILogger<ProductReviewReceivedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductReviewReceivedIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        _logger.LogInformation(
            "[ReviewConsumer] Yeni cevapsiz review: ReviewId={ReviewId}, ProductId={ProductId}, Rating={Rating}, Platform={Platform}",
            msg.ReviewId, msg.ProductId, msg.Rating, msg.PlatformCode);

        // Dusuk puan (1-2 yildiz) ise uyari seviyesi yukselt
        var level = msg.Rating <= 2 ? NotificationLevel.Warning : NotificationLevel.Info;
        var title = msg.Rating <= 2
            ? $"Dusuk Puan Uyarisi — {msg.PlatformCode} Review #{msg.ReviewId}"
            : $"Yeni Review — {msg.PlatformCode} #{msg.ReviewId}";

        var message = $"Urun #{msg.ProductId} icin {msg.Rating}/5 yildiz degerlendirme alindi.\n" +
                      $"Yorum: {(msg.Comment.Length > 200 ? msg.Comment[..200] + "..." : msg.Comment)}";

        await _notificationService.NotifyAsync(title, message, level, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[ReviewConsumer] Bildirim gonderildi: ReviewId={ReviewId}, Level={Level}",
            msg.ReviewId, level);
    }
}
