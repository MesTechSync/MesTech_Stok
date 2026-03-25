using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Fatura onaylandiginda GL yevmiye kaydi olusturur (Zincir 3).
/// Cift kayit: BORC 120 Alicilar, ALACAK 600 Satislar + 391 KDV.
/// </summary>
public interface IInvoiceApprovedGLHandler
{
    Task HandleAsync(
        Guid invoiceId, Guid tenantId, string invoiceNumber,
        decimal grandTotal, decimal taxAmount, decimal netAmount,
        CancellationToken ct);
}

public sealed class InvoiceApprovedGLHandler : IInvoiceApprovedGLHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InvoiceApprovedGLHandler> _logger;

    public InvoiceApprovedGLHandler(
        IUnitOfWork unitOfWork,
        ILogger<InvoiceApprovedGLHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid invoiceId, Guid tenantId, string invoiceNumber,
        decimal grandTotal, decimal taxAmount, decimal netAmount,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "GL kaydi olusturuluyor — Fatura={InvoiceNumber}, Toplam={Total}, KDV={Tax}, Net={Net}",
            invoiceNumber, grandTotal, taxAmount, netAmount);

        var entry = JournalEntry.Create(
            tenantId,
            DateTime.UtcNow,
            $"Satis faturasi #{invoiceNumber}",
            invoiceNumber);

        // Cift kayit: Borc = Alacak
        // BORC: 120 Alicilar (musteri bize borclu)
        entry.AddLine(AccountingConstants.Account120Receivables, grandTotal, 0, $"120 Alicilar — Fatura #{invoiceNumber}");

        // ALACAK: 600 Satislar (gelir)
        entry.AddLine(AccountingConstants.Account600DomesticSales, 0, netAmount, $"600 Yurtici Satislar — Fatura #{invoiceNumber}");

        // ALACAK: 391 KDV (devlete borc)
        if (taxAmount > 0)
            entry.AddLine(AccountingConstants.Account391VatPayable, 0, taxAmount, $"391 Hesaplanan KDV — Fatura #{invoiceNumber}");

        entry.Validate();
        entry.Post();

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "GL kaydi olusturuldu — Fatura={InvoiceNumber}, JournalEntryId={EntryId}, Borc={Debit}, Alacak={Credit}",
            invoiceNumber, entry.Id, grandTotal, grandTotal);
    }
}
