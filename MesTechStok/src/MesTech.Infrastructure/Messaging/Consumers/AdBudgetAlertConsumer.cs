using MassTransit;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Consumers;

/// <summary>
/// Reklam kampanya butce asimi uyarisi consumer'i.
/// TrendyolAdsSyncJob tarafindan publish edilen event'leri tuketir.
/// Butce %80'i astiginda Critical bildirim gonderir.
/// </summary>
public sealed class AdBudgetAlertConsumer : IConsumer<AdBudgetAlertIntegrationEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<AdBudgetAlertConsumer> _logger;

    public AdBudgetAlertConsumer(
        INotificationService notificationService,
        ILogger<AdBudgetAlertConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AdBudgetAlertIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        _logger.LogWarning(
            "[AdBudgetConsumer] Butce uyarisi: CampaignId={CampaignId}, Name={Name}, Harcama={Spent:P0}, Platform={Platform}",
            msg.CampaignId, msg.CampaignName, msg.SpentPercentage / 100m, msg.PlatformCode);

        var level = msg.SpentPercentage >= 95 ? NotificationLevel.Critical : NotificationLevel.Warning;
        var title = $"Reklam Butce Uyarisi — {msg.PlatformCode} '{msg.CampaignName}'";

        var message = $"Kampanya '{msg.CampaignName}' (ID: {msg.CampaignId})\n" +
                      $"Gunluk Butce: {msg.DailyBudget:C}\n" +
                      $"Toplam Harcama: {msg.TotalSpent:C}\n" +
                      $"Kullanim: %{msg.SpentPercentage:F1}\n" +
                      $"{(msg.SpentPercentage >= 95 ? "KRITIK: Butce tukenmek uzere!" : "UYARI: Butce limitine yaklasiliyor.")}";

        await _notificationService.NotifyAsync(title, message, level, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[AdBudgetConsumer] Bildirim gonderildi: CampaignId={CampaignId}, Level={Level}",
            msg.CampaignId, level);
    }
}
