using System.Text.Json;
using System.Text.Json.Nodes;
using MassTransit;
using MediatR;
using MesTech.Application.Commands.ApproveAccountingEntry;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// WhatsApp/Panel uzerinden onaylanan muhasebe belgesini consume eder.
/// Onaylanan belgeden PersonalExpense + JournalEntry olusturur.
/// JournalEntry: Borc (gider hesabi) = Alacak (kasa/banka hesabi).
/// Post() sonrasi LedgerPostedEvent tetiklenir → AnomalyCheckHandler devreye girer.
/// </summary>
public sealed class AccountingApprovalConsumer : IConsumer<BotAccountingApprovedEvent>
{
    private readonly IMediator _mediator;
    private readonly IAccountingDocumentRepository _documentRepository;
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IPersonalExpenseRepository _expenseRepository;
    private readonly IChartOfAccountsRepository _chartOfAccountsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMesaEventMonitor _monitor;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<AccountingApprovalConsumer> _logger;

    /// <summary>
    /// Varsayilan gider hesabi kodu: 770 Genel Yonetim Giderleri (THP).
    /// </summary>
    private const string DefaultExpenseAccountCode = "770";

    /// <summary>
    /// Varsayilan odeme hesabi kodu: 100 Kasa (THP).
    /// </summary>
    private const string DefaultCashAccountCode = "100";

    public AccountingApprovalConsumer(
        IMediator mediator,
        IAccountingDocumentRepository documentRepository,
        IJournalEntryRepository journalEntryRepository,
        IPersonalExpenseRepository expenseRepository,
        IChartOfAccountsRepository chartOfAccountsRepository,
        IUnitOfWork unitOfWork,
        IMesaEventMonitor monitor,
        ITenantProvider tenantProvider,
        ILogger<AccountingApprovalConsumer> logger)
    {
        _mediator = mediator;
        _documentRepository = documentRepository;
        _journalEntryRepository = journalEntryRepository;
        _expenseRepository = expenseRepository;
        _chartOfAccountsRepository = chartOfAccountsRepository;
        _unitOfWork = unitOfWork;
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

        if (tenantId == Guid.Empty)
        {
            _logger.LogError(
                "[MESA Consumer] TenantId is Guid.Empty after fallback — aborting to prevent cross-tenant data leak. MessageId={MessageId}",
                context.MessageId);
            _monitor.RecordError("bot.accounting.approved", "TenantId is Guid.Empty — aborted");
            throw new InvalidOperationException("TenantId is Guid.Empty — message rejected to prevent cross-tenant data leak");
        }

        _logger.LogInformation(
            "Processing {Event} — {Id}",
            nameof(BotAccountingApprovedEvent), context.MessageId);

        try
        {
            await _mediator.Send(new ApproveAccountingEntryCommand
            {
                DocumentId = msg.DocumentId,
                ApprovedBy = msg.ApprovedBy,
                ApprovalSource = msg.ApprovalSource,
                JournalEntryId = msg.JournalEntryId,
                TenantId = tenantId
            }, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to process {Event}", nameof(BotAccountingApprovedEvent));
            throw; // Let MassTransit retry policy handle
        }

        _logger.LogInformation(
            "[MESA Consumer] Muhasebe belgesi onaylandi: DocId={DocumentId}, onaylayan={ApprovedBy}, kaynak={ApprovalSource}",
            msg.DocumentId, msg.ApprovedBy, msg.ApprovalSource);

        // Belgeyi bul ve cikarilmis veriyi oku
        var document = await _documentRepository.GetByIdAsync(msg.DocumentId).ConfigureAwait(false);
        if (document is null)
        {
            _logger.LogWarning(
                "[MESA Consumer] Onaylanan belge bulunamadi: DocId={DocumentId}", msg.DocumentId);
            _monitor.RecordError("bot.accounting.approved", $"Document not found: {msg.DocumentId}");
            return;
        }

        Guid? journalEntryId = null;

        // JournalEntry baglantisi zaten varsa atla
        if (msg.JournalEntryId.HasValue && msg.JournalEntryId.Value != Guid.Empty)
        {
            journalEntryId = msg.JournalEntryId.Value;
            _logger.LogInformation(
                "[MESA Consumer] JournalEntry zaten mevcut: {JournalEntryId}, belge baglantisi kontrol ediliyor",
                msg.JournalEntryId);
        }
        else
        {
            // Belgeden tutar cikar
            var amount = ExtractAmountFromDocument(document);
            if (amount > 0)
            {
                journalEntryId = await CreateExpenseAndJournalEntryAsync(
                    tenantId, document, amount, msg.ApprovedBy, context.CancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning(
                    "[MESA Consumer] Belge tutari cikarilmadi veya sifir: DocId={DocumentId}. " +
                    "JournalEntry oluşturulmadi — manuel islem gerekli.",
                    msg.DocumentId);
            }
        }

        // Belgeye onay bilgisi ekle (JsonNode ile güvenli birleştirme)
        var approvalNode = new JsonObject
        {
            ["Status"] = "Approved",
            ["ApprovedBy"] = msg.ApprovedBy,
            ["ApprovalSource"] = msg.ApprovalSource,
            ["JournalEntryId"] = journalEntryId?.ToString(),
            ["ApprovedAt"] = msg.OccurredAt
        };

        JsonNode? existingNode;
        try
        {
            existingNode = JsonNode.Parse(document.ExtractedData ?? "{}");
        }
        catch (JsonException)
        {
            existingNode = new JsonObject();
        }

        var combinedNode = new JsonObject
        {
            ["extraction"] = existingNode,
            ["approval"] = approvalNode
        };
        document.UpdateExtractedData(combinedNode.ToJsonString());
        await _documentRepository.UpdateAsync(document, context.CancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "[MESA Consumer] Belge onay bilgisi kaydedildi: DocId={DocumentId}", msg.DocumentId);

        _monitor.RecordConsume("bot.accounting.approved");
    }

    /// <summary>
    /// PersonalExpense + JournalEntry olusturur ve deftere isler.
    /// </summary>
    private async Task<Guid?> CreateExpenseAndJournalEntryAsync(
        Guid tenantId,
        AccountingDocument document,
        decimal amount,
        string approvedBy,
        CancellationToken ct)
    {
        try
        {
            // 1. PersonalExpense olustur
            var expense = PersonalExpense.Create(
                tenantId,
                title: $"Belge onay gideri: {document.FileName}",
                amount: amount,
                expenseDate: DateTime.UtcNow,
                source: ExpenseSource.AI,
                category: document.DocumentType.ToString());

            expense.Approve(approvedBy);
            await _expenseRepository.AddAsync(expense, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[MESA Consumer] PersonalExpense olusturuldu: ExpenseId={ExpenseId}, tutar={Amount:N2}",
                expense.Id, amount);

            // 2. Hesap planından gider ve kasa hesaplarini bul
            var expenseAccount = await _chartOfAccountsRepository.GetByCodeAsync(
                tenantId, DefaultExpenseAccountCode, ct).ConfigureAwait(false);
            var cashAccount = await _chartOfAccountsRepository.GetByCodeAsync(
                tenantId, DefaultCashAccountCode, ct).ConfigureAwait(false);

            if (expenseAccount is null || cashAccount is null)
            {
                _logger.LogWarning(
                    "[MESA Consumer] Hesap plani eksik (770 veya 100 bulunamadi). " +
                    "JournalEntry olusturulmadi — sadece Expense kaydedildi. DocId={DocumentId}",
                    document.Id);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return null;
            }

            // 3. JournalEntry olustur: Borc 770 (gider) = Alacak 100 (kasa)
            var journalEntry = JournalEntry.Create(
                tenantId,
                entryDate: DateTime.UtcNow,
                description: $"Belge onay: {document.FileName} ({document.DocumentType})",
                referenceNumber: $"DOC-{document.Id:N}".Substring(0, 20));

            journalEntry.AddLine(expenseAccount.Id, debit: amount, credit: 0m,
                description: $"Gider: {document.DocumentType}");
            journalEntry.AddLine(cashAccount.Id, debit: 0m, credit: amount,
                description: $"Kasa cikisi: {document.FileName}");

            // 4. Validate (Borc = Alacak kurali) ve Post (deftere isle)
            journalEntry.Validate();
            journalEntry.Post(); // LedgerPostedEvent tetiklenir → AnomalyCheckHandler

            await _journalEntryRepository.AddAsync(journalEntry, ct).ConfigureAwait(false);

            // 5. UnitOfWork ile tum degisiklikleri kaydet
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[MESA Consumer] JournalEntry olusturuldu ve deftere islendi: " +
                "JournalEntryId={JournalEntryId}, tutar={Amount:N2}, DocId={DocumentId}",
                journalEntry.Id, amount, document.Id);

            return journalEntry.Id;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "[MESA Consumer] Expense/JournalEntry olusturma hatasi: DocId={DocumentId}. " +
                "Re-throwing to prevent silent ACK — MassTransit will retry or DLQ.",
                document.Id);
            throw; // P0 fix: silent null return caused ACKed messages with no JournalEntry
        }
    }

    /// <summary>
    /// AccountingDocument.ExtractedData JSON'undan tutar cikarir.
    /// Oncelik: document.Amount → ExtractedData.Amount/ExtractedAmount
    /// </summary>
    private decimal ExtractAmountFromDocument(AccountingDocument document)
    {
        // Oncelik 1: AccountingDocument.Amount
        if (document.Amount.HasValue && document.Amount.Value > 0)
            return document.Amount.Value;

        // Oncelik 2: ExtractedData JSON parse
        if (string.IsNullOrWhiteSpace(document.ExtractedData))
            return 0m;

        try
        {
            using var jsonDoc = JsonDocument.Parse(document.ExtractedData);
            var root = jsonDoc.RootElement;

            // ExtractedAmount (AI classified event format)
            if (root.TryGetProperty("ExtractedAmount", out var extractedAmount)
                && extractedAmount.ValueKind == JsonValueKind.Number)
            {
                return extractedAmount.GetDecimal();
            }

            // Amount (extraction format)
            if (root.TryGetProperty("Amount", out var amountProp)
                && amountProp.ValueKind == JsonValueKind.Number)
            {
                return amountProp.GetDecimal();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "[MESA Consumer] ExtractedData JSON parse hatasi: DocId={DocumentId}",
                document.Id);
        }

        return 0m;
    }
}
