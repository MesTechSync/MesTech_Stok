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
    private readonly ILogger<InvoiceCancelledReversalHandler> _logger;

    public InvoiceCancelledReversalHandler(
        IUnitOfWork unitOfWork,
        ILogger<InvoiceCancelledReversalHandler> logger)
    {
        _unitOfWork = unitOfWork;
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

        _logger.LogWarning(
            "Fatura iptal — ters GL kaydi olusturuluyor. Invoice={InvoiceNumber}, Reason={Reason}",
            invoiceNumber, reason);

        // Ters kayit: orijinalin tam tersi
        var entry = JournalEntry.Create(
            tenantId,
            DateTime.UtcNow,
            $"IPTAL: Fatura #{invoiceNumber} — {reason ?? "Sebep belirtilmedi"}",
            $"REV-{invoiceNumber}");

        // KDV oranı varsayılan %20 — gerçek oran fatura entity'den alınmalı
        var taxRate = 0.20m;
        var netAmount = grandTotal / (1 + taxRate);
        var taxAmount = grandTotal - netAmount;

        // TERS: BORC 600 Satislar (gelir iptali)
        entry.AddLine(AccountingConstants.Account600DomesticSales, netAmount, 0, $"600 Satislar — IPTAL #{invoiceNumber}");

        // TERS: BORC 391 KDV (KDV iptali)
        if (taxAmount > 0)
            entry.AddLine(AccountingConstants.Account391VatPayable, taxAmount, 0, $"391 KDV — IPTAL #{invoiceNumber}");

        // TERS: ALACAK 120 Alicilar (musteri borcu silinir)
        entry.AddLine(AccountingConstants.Account120Receivables, 0, grandTotal, $"120 Alicilar — IPTAL #{invoiceNumber}");

        entry.Validate();
        entry.Post();

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Ters GL tamamlandi — Invoice={InvoiceNumber}, TersKayit={EntryId}, Tutar={Total}",
            invoiceNumber, entry.Id, grandTotal);
    }
}
