using MassTransit;
using MediatR;
using MesTech.Application.Commands.UpdateDocumentCategory;
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
public sealed class DocumentClassifiedConsumer : IConsumer<AiDocumentClassifiedEvent>
{
    private readonly IMediator _mediator;
    private readonly IAccountingDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DocumentClassifiedConsumer> _logger;

    public DocumentClassifiedConsumer(
        IMediator mediator,
        IAccountingDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<DocumentClassifiedConsumer> logger)
    {
        _mediator = mediator;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
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

        if (tenantId == Guid.Empty)
        {
            _logger.LogError(
                "[MESA Consumer] TenantId is Guid.Empty after fallback — aborting. MessageId={MessageId}",
                context.MessageId);
            _monitor.RecordError("ai.document.classified", "TenantId is Guid.Empty — aborted");
            return;
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(AiDocumentClassifiedEvent), context.MessageId);

        try
        {
            await _mediator.Send(new UpdateDocumentCategoryCommand
            {
                DocumentId = msg.DocumentId,
                DocumentType = msg.DocumentType,
                Confidence = msg.Confidence,
                ExtractedAmount = msg.ExtractedAmount,
                ExtractedVKN = msg.ExtractedVKN,
                TenantId = tenantId
            }, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(AiDocumentClassifiedEvent));
            throw; // Let MassTransit retry policy handle
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
        var document = await _documentRepository.GetByIdAsync(msg.DocumentId).ConfigureAwait(false);
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
            await _documentRepository.UpdateAsync(document, context.CancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

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
