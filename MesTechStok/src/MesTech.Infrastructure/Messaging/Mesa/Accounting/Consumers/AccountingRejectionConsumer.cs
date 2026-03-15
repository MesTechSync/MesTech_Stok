using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// WhatsApp/Panel uzerinden reddedilen muhasebe belgesini consume eder.
/// Belge status bilgisini gunceller ve red sebebini kaydeder.
/// </summary>
public class AccountingRejectionConsumer : IConsumer<BotAccountingRejectedEvent>
{
    private readonly IAccountingDocumentRepository _documentRepository;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AccountingRejectionConsumer> _logger;

    public AccountingRejectionConsumer(
        IAccountingDocumentRepository documentRepository,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<AccountingRejectionConsumer> logger)
    {
        _documentRepository = documentRepository;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BotAccountingRejectedEvent> context)
    {
        var msg = context.Message;
        var tenantId = msg.TenantId;
        if (tenantId == Guid.Empty)
        {
            tenantId = _tenantProvider.GetCurrentTenantId();
            _logger.LogWarning(
                "[MESA Consumer] Rejection event without TenantId, using default {TenantId}", tenantId);
        }

        _logger.LogInformation(
            "[MESA Consumer] Muhasebe belgesi reddedildi: DocId={DocumentId}, reddeden={RejectedBy}, sebep={Reason}",
            msg.DocumentId, msg.RejectedBy, msg.Reason);

        // Belgeyi bul
        var document = await _documentRepository.GetByIdAsync(msg.DocumentId);
        if (document is null)
        {
            _logger.LogWarning(
                "[MESA Consumer] Reddedilen belge bulunamadi: DocId={DocumentId}", msg.DocumentId);
            _monitor.RecordError("bot.accounting.rejected", $"Document not found: {msg.DocumentId}");
            return;
        }

        // Belgeye red bilgisi ekle (ExtractedData JSON'una)
        var rejectionJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            Status = "Rejected",
            msg.RejectedBy,
            msg.RejectionSource,
            msg.Reason,
            RejectedAt = msg.OccurredAt
        });

        var existingData = document.ExtractedData ?? "{}";
        var combinedData = $"{{\"extraction\":{existingData},\"rejection\":{rejectionJson}}}";
        document.UpdateExtractedData(combinedData);
        await _documentRepository.UpdateAsync(document);

        _logger.LogInformation(
            "[MESA Consumer] Belge red bilgisi kaydedildi: DocId={DocumentId}, sebep={Reason}",
            msg.DocumentId, msg.Reason);

        _monitor.RecordConsume("bot.accounting.rejected");
    }
}
