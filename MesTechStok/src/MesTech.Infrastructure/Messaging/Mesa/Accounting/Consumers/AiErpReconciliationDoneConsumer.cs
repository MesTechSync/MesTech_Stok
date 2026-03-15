using MassTransit;
using MesTech.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA AI ERP uzlastirmasi tamamladi — uyusmazliklar raporlanir.
/// Dalga 9: Handler logic Sprint D'de eklenecek — simdilik log.
/// </summary>
public class AiErpReconciliationDoneConsumer : IConsumer<AiErpReconciliationDoneIntegrationEvent>
{
    private readonly ILogger<AiErpReconciliationDoneConsumer> _logger;

    public AiErpReconciliationDoneConsumer(ILogger<AiErpReconciliationDoneConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<AiErpReconciliationDoneIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[MESA Consumer] AI ERP uzlastirma tamamlandi: ErpProvider={ErpProvider}, " +
            "ReconciledCount={ReconciledCount}, MismatchCount={MismatchCount}, TenantId={TenantId}",
            msg.ErpProvider, msg.ReconciledCount, msg.MismatchCount, msg.TenantId);
        // Handler logic Sprint D'de eklenecek — simdilik log
        return Task.CompletedTask;
    }
}
