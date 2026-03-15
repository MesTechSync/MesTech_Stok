using MassTransit;
using MesTech.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA AI e-fatura taslagi olusturdu — muhasebe onayina gonder.
/// Dalga 9: Handler logic Sprint D'de eklenecek — simdilik log.
/// </summary>
public class AiEInvoiceDraftGeneratedConsumer : IConsumer<AiEInvoiceDraftGeneratedIntegrationEvent>
{
    private readonly ILogger<AiEInvoiceDraftGeneratedConsumer> _logger;

    public AiEInvoiceDraftGeneratedConsumer(ILogger<AiEInvoiceDraftGeneratedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<AiEInvoiceDraftGeneratedIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[MESA Consumer] AI e-fatura taslagi alindi: OrderId={OrderId}, " +
            "SuggestedEttn={SuggestedEttnNo}, SuggestedTotal={SuggestedTotal:F2}, TenantId={TenantId}",
            msg.OrderId, msg.SuggestedEttnNo, msg.SuggestedTotal, msg.TenantId);
        // Handler logic Sprint D'de eklenecek — simdilik log
        return Task.CompletedTask;
    }
}
