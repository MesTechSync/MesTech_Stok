using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Fatura iptal edildiginde ters yevmiye kaydi olusturur (Zincir 4).
/// Orijinal GL kaydinin tersini yazar: borc→alacak, alacak→borc.
/// </summary>
public interface IInvoiceCancelledReversalHandler
{
    Task HandleAsync(
        Guid invoiceId, Guid orderId, string invoiceNumber,
        string? reason, Guid tenantId, decimal grandTotal,
        CancellationToken ct);
}

public sealed class InvoiceCancelledReversalHandler : IInvoiceCancelledReversalHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalEntryRepository _journalRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ILogger<InvoiceCancelledReversalHandler> _logger;

    public InvoiceCancelledReversalHandler(
        IUnitOfWork unitOfWork,
        IJournalEntryRepository journalRepo,
        IInvoiceRepository invoiceRepo,
        ILogger<InvoiceCancelledReversalHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _journalRepo = journalRepo;
        _invoiceRepo = invoiceRepo;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid invoiceId, Guid orderId, string invoiceNumber,
        string? reason, Guid tenantId, decimal grandTotal,
        CancellationToken ct)
    {
        if (grandTotal <= 0)
        {
            _logger.LogDebug("Fatura tutarı 0 — ters GL kaydi atlanıyor. InvoiceId={InvoiceId}", invoiceId);
            return;
        }

        var refNumber = $"CANCEL-{invoiceNumber}";
        if (await _journalRepo.ExistsByReferenceAsync(tenantId, refNumber, ct))
        {
            _logger.LogWarning("Duplicate iptal GL — ref {Ref} zaten var, atlanıyor", refNumber);
            return;
        }

        _logger.LogWarning(
            "Fatura iptal — ters GL kaydi olusturuluyor. Invoice={InvoiceNumber}, Reason={Reason}",
            invoiceNumber, reason);

        // Ters kayit: orijinalin tam tersi
        var entry = JournalEntry.Create(
            tenantId,
            DateTime.UtcNow,
            $"IPTAL: Fatura #{invoiceNumber} — {reason ?? "Sebep belirtilmedi"}",
            $"REV-{invoiceNumber}");

        // G137 FIX: Gerçek KDV tutarını faturadan çek — hardcoded %20 MALİ HATA (G137)
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId, ct).ConfigureAwait(false);
        var taxAmount = invoice?.TaxTotal ?? Math.Round(grandTotal * 0.20m / 1.20m, 2);
        var netAmount = grandTotal - taxAmount;

        // TERS: BORC 600 Satislar (gelir iptali)
        entry.AddLine(AccountingConstants.Account600DomesticSales, netAmount, 0, $"600 Satislar — IPTAL #{invoiceNumber}");

        // TERS: BORC 391 KDV (KDV iptali)
        if (taxAmount > 0)
            entry.AddLine(AccountingConstants.Account391VatPayable, taxAmount, 0, $"391 KDV — IPTAL #{invoiceNumber}");

        // TERS: ALACAK 120 Alicilar (musteri borcu silinir)
        entry.AddLine(AccountingConstants.Account120Receivables, 0, grandTotal, $"120 Alicilar — IPTAL #{invoiceNumber}");

        entry.Validate();
        entry.Post();

        await _journalRepo.AddAsync(entry, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Ters GL tamamlandi — Invoice={InvoiceNumber}, TersKayit={EntryId}, Tutar={Total}",
            invoiceNumber, entry.Id, grandTotal);
    }
}
