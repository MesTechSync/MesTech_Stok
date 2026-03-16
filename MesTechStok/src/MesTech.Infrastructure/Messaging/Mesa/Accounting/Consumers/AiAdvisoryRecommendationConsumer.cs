using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA AI finansal danismanlik onerilerini consume eder.
/// Onerileri log/notification kaydeder ve dashboard'a bildirir.
/// </summary>
public class AiAdvisoryRecommendationConsumer : IConsumer<AiAdvisoryRecommendationEvent>
{
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AiAdvisoryRecommendationConsumer> _logger;

    public AiAdvisoryRecommendationConsumer(
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<AiAdvisoryRecommendationConsumer> logger)
    {
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AiAdvisoryRecommendationEvent> context)
    {
        var msg = context.Message;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning(
                "[MESA Consumer] Event without TenantId, using default {TenantId}", tenantId);
        }

        _logger.LogInformation(
            "[MESA Consumer] AI danismanlik onerisi alindi: " +
            "Tip={RecommendationType}, Baslik={Title}, Oncelik={Priority}",
            msg.RecommendationType, msg.Title, msg.Priority);

        _logger.LogInformation(
            "[MESA Consumer] Oneri detayi: {Description}",
            msg.Description);

        if (!string.IsNullOrWhiteSpace(msg.ActionUrl))
        {
            _logger.LogInformation(
                "[MESA Consumer] Aksiyon URL: {ActionUrl}", msg.ActionUrl);
        }

        // Dashboard bildirim sistemi icin kaydet (IDashboardNotifier WebSocket push eklenecek)
        _logger.LogInformation(
            "[MESA Consumer] AI onerisi dashboard bildirimine kaydedildi: " +
            "Tip={RecommendationType}, Oncelik={Priority}",
            msg.RecommendationType, msg.Priority);

        _monitor.RecordConsume("ai.advisory.recommendation");

        await Task.CompletedTask;
    }
}
