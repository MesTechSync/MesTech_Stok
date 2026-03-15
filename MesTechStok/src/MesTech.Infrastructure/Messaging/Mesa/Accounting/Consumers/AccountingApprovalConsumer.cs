using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// WhatsApp/Panel uzerinden onaylanan muhasebe belgesini consume eder.
/// Onaylanan belgeden JournalEntry olusturur.
/// </summary>
public class AccountingApprovalConsumer : IConsumer<BotAccountingApprovedEvent>
{
    private readonly IAccountingDocumentRepository _documentRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AccountingApprovalConsumer> _logger;

    public AccountingApprovalConsumer(
        IAccountingDocumentRepository documentRepository,
        IJournalEntryRepository journalEntryRepository,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<AccountingApprovalConsumer> logger)
    {
        _documentRepository = documentRepository;
        _journalEntryRepository = journalEntryRepository;
        _monitor = monitor;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BotAccountingApprovedEvent> context)
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
            "[MESA Consumer] Muhasebe belgesi onaylandi: DocId={DocumentId}, onaylayan={ApprovedBy}, kaynak={ApprovalSource}",
            msg.DocumentId, msg.ApprovedBy, msg.ApprovalSource);

        // Belgeyi bul ve cikarilmis veriyi oku
        var document = await _documentRepository.GetByIdAsync(msg.DocumentId);
        if (document is null)
        {
            _logger.LogWarning(
                "[MESA Consumer] Onaylanan belge bulunamadi: DocId={DocumentId}", msg.DocumentId);
            _monitor.RecordError("bot.accounting.approved", $"Document not found: {msg.DocumentId}");
            return;
        }

        // JournalEntry baglantisi zaten varsa atla
        if (msg.JournalEntryId.HasValue && msg.JournalEntryId.Value != Guid.Empty)
        {
            _logger.LogInformation(
                "[MESA Consumer] JournalEntry zaten mevcut: {JournalEntryId}, belge baglantisi kontrol ediliyor",
                msg.JournalEntryId);
        }
        else
        {
            // TODO: JournalEntry olustur — Dalga 2'de aktif edilecek
            // Simdilik sadece log ile bildiriyoruz
            _logger.LogInformation(
                "[MESA Consumer] JournalEntry olusturulmali: DocId={DocumentId}, onaylayan={ApprovedBy}. " +
                "Dalga 2'de otomatik yevmiye kaydi olusturulacak.",
                msg.DocumentId, msg.ApprovedBy);
        }

        // Belgeye onay bilgisi ekle
        var approvalJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            msg.ApprovedBy,
            msg.ApprovalSource,
            msg.JournalEntryId,
            ApprovedAt = msg.OccurredAt
        });

        var existingData = document.ExtractedData ?? "{}";
        var combinedData = $"{{\"extraction\":{existingData},\"approval\":{approvalJson}}}";
        document.UpdateExtractedData(combinedData);
        await _documentRepository.UpdateAsync(document);

        _logger.LogInformation(
            "[MESA Consumer] Belge onay bilgisi kaydedildi: DocId={DocumentId}", msg.DocumentId);

        _monitor.RecordConsume("bot.accounting.approved");
    }
}
