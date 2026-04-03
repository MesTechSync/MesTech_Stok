using MassTransit;
using MediatR;
using MesTech.Application.Commands.UpdateDocumentMetadata;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// MESA AI belge icerik cikarimi sonucunu consume eder.
/// AccountingDocument.ProcessedJson gunceller ve status = "Extracted".
/// Confidence >= 0.90 ise otomatik Expense olusturur (PendingApproval).
/// </summary>
public sealed class AiDocumentExtractedConsumer : IConsumer<AiDocumentExtractedEvent>
{
    private readonly IMediator _mediator;
    private readonly IAccountingDocumentRepository _documentRepository;
    private readonly IPersonalExpenseRepository _expenseRepository;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AiDocumentExtractedConsumer> _logger;

    public AiDocumentExtractedConsumer(
        IMediator mediator,
        IAccountingDocumentRepository documentRepository,
        IPersonalExpenseRepository expenseRepository,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ILogger<AiDocumentExtractedConsumer> logger)
    {
        _mediator = mediator;
        _documentRepository = documentRepository;
        _expenseRepository = expenseRepository;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AiDocumentExtractedEvent> context)
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
            _monitor.RecordError("ai.document.extracted", "TenantId is Guid.Empty — aborted");
            return;
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(AiDocumentExtractedEvent), context.MessageId);

        try
        {
            await _mediator.Send(new UpdateDocumentMetadataCommand
            {
                DocumentId = msg.DocumentId,
                ProcessedJson = msg.ProcessedJson,
                Confidence = msg.Confidence,
                ExtractedAmount = msg.ExtractedAmount,
                ExtractedVKN = msg.ExtractedVKN,
                ExtractedCategory = msg.ExtractedCategory,
                TenantId = tenantId
            }, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(AiDocumentExtractedEvent));
            throw; // Let MassTransit retry policy handle
        }

        _logger.LogInformation(
            "[MESA Consumer] AI belge cikarimi alindi: DocId={DocumentId}, guven={Confidence:P0}",
            msg.DocumentId, msg.Confidence);

        // Belgeyi bul
        var document = await _documentRepository.GetByIdAsync(msg.DocumentId).ConfigureAwait(false);
        if (document is null)
        {
            _logger.LogWarning(
                "[MESA Consumer] Cikarimi alinan belge bulunamadi: DocId={DocumentId}", msg.DocumentId);
            _monitor.RecordError("ai.document.extracted", $"Document not found: {msg.DocumentId}");
            return;
        }

        // ExtractedData guncelle
        var extractedJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            Status = "Extracted",
            msg.ProcessedJson,
            msg.Confidence,
            msg.ExtractedAmount,
            msg.ExtractedVKN,
            msg.ExtractedCategory,
            ExtractedAt = msg.OccurredAt
        });

        document.UpdateExtractedData(extractedJson);
        await _documentRepository.UpdateAsync(document, context.CancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "[MESA Consumer] AccountingDocument guncellendi (Extracted): DocId={DocumentId}", msg.DocumentId);

        // Yuksek guven skoru — otomatik gider kaydı olustur
        if (msg.Confidence >= 0.90m && msg.ExtractedAmount.HasValue && msg.ExtractedAmount.Value > 0)
        {
            var expense = PersonalExpense.Create(
                tenantId: tenantId,
                title: $"AI-Extracted: {document.FileName}",
                amount: msg.ExtractedAmount.Value,
                expenseDate: DateTime.UtcNow,
                source: Domain.Accounting.Enums.ExpenseSource.AI,
                category: msg.ExtractedCategory ?? "Genel");

            await _expenseRepository.AddAsync(expense, context.CancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "[MESA Consumer] Otomatik gider kaydı olusturuldu (PendingApproval): " +
                "DocId={DocumentId}, tutar={Amount:F2}, kategori={Category}",
                msg.DocumentId, msg.ExtractedAmount.Value, msg.ExtractedCategory ?? "Genel");
        }

        _monitor.RecordConsume("ai.document.extracted");
    }
}
