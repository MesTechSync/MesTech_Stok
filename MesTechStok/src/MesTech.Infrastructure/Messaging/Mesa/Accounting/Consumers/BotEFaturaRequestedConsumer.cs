using MassTransit;
using MesTech.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA Bot e-fatura talep etti — WhatsApp/Telegram kanali mekanizmasi.
/// Dalga 9: Handler logic Sprint D'de eklenecek — simdilik log.
/// </summary>
public class BotEFaturaRequestedConsumer : IConsumer<BotEFaturaRequestedIntegrationEvent>
{
    private readonly ILogger<BotEFaturaRequestedConsumer> _logger;

    public BotEFaturaRequestedConsumer(ILogger<BotEFaturaRequestedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<BotEFaturaRequestedIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[MESA Consumer] Bot e-fatura talebi geldi: BotUserId={BotUserId}, " +
            "OrderId={OrderId}, BuyerVkn={BuyerVkn}, TenantId={TenantId}",
            msg.BotUserId, msg.OrderId, msg.BuyerVkn, msg.TenantId);
        // Handler logic Sprint D'de eklenecek — simdilik log
        return Task.CompletedTask;
    }
}
