using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA AI belge siniflandirma sonucunu consume eder.
/// AccountingDocument status gunceller ve cikarilan verileri kaydeder.
/// </summary>
public class DocumentClassifiedConsumer : IConsumer<AiDocumentClassifiedEvent>
{
    private readonly IAccountingDocumentRepository _documentRepository;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DocumentClassifiedConsumer> _logger;

    public DocumentClassifiedConsumer(
        IAccountingDocumentRepository documentRepository,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<DocumentClassifiedConsumer> logger)
    {
        _documentRepository = documentRepository;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AiDocumentClassifiedEvent> context)
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
            "[MESA Consumer] AI belge siniflandirma alindi: DocId={DocumentId}, tip={DocumentType}, guven={Confidence:P0}",
            msg.DocumentId, msg.DocumentType, msg.Confidence);

        if (msg.ExtractedAmount.HasValue)
        {
            _logger.LogInformation(
                "[MESA Consumer] Cikarilan tutar: {Amount:N2} TL, VKN: {VKN}",
                msg.ExtractedAmount, msg.ExtractedVKN ?? "-");
        }

        // AccountingDocument.ExtractedData guncelle
        var document = await _documentRepository.GetByIdAsync(msg.DocumentId);
        if (document is not null)
        {
            var extractedJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                msg.DocumentType,
                msg.Confidence,
                msg.ExtractedAmount,
                msg.ExtractedVKN,
                ClassifiedAt = msg.OccurredAt
            });

            document.UpdateExtractedData(extractedJson);
            await _documentRepository.UpdateAsync(document);

            _logger.LogInformation(
                "[MESA Consumer] AccountingDocument guncellendi: DocId={DocumentId}", msg.DocumentId);
        }
        else
        {
            _logger.LogWarning(
                "[MESA Consumer] AccountingDocument bulunamadi: DocId={DocumentId}", msg.DocumentId);
        }

        _monitor.RecordConsume("ai.document.classified");
    }
}
