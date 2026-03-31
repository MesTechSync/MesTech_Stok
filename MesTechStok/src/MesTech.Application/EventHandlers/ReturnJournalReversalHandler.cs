using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// İade onaylandığında ters muhasebe kaydı (Zincir 5 — GL bacağı).
/// ReturnApprovedEvent tetikler → JournalEntry ters kayıt (610 Satıştan İadeler / 120 Alıcılar).
/// </summary>
public interface IReturnJournalReversalHandler
{
    Task HandleAsync(
        Guid returnRequestId, Guid orderId, Guid tenantId,
        decimal totalRefundAmount,
        CancellationToken ct);
}

public sealed class ReturnJournalReversalHandler : IReturnJournalReversalHandler
{
    private readonly IUnitOfWork _uow;
    private readonly IJournalEntryRepository _journalRepo;
    private readonly ILogger<ReturnJournalReversalHandler> _logger;

    public ReturnJournalReversalHandler(
        IUnitOfWork uow,
        IJournalEntryRepository journalRepo,
        ILogger<ReturnJournalReversalHandler> logger)
    {
        _uow = uow;
        _journalRepo = journalRepo;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid returnRequestId, Guid orderId, Guid tenantId,
        decimal totalRefundAmount,
        CancellationToken ct)
    {
        if (totalRefundAmount <= 0)
        {
            _logger.LogDebug("İade tutarı 0 — GL ters kayıt atlanıyor. ReturnId={ReturnId}", returnRequestId);
            return;
        }

        var refNumber = $"RET-{returnRequestId.ToString()[..8]}";
        if (await _journalRepo.ExistsByReferenceAsync(tenantId, refNumber, ct))
        {
            _logger.LogWarning("Duplicate iade GL — ref {Ref} zaten var, atlanıyor", refNumber);
            return;
        }

        var entry = JournalEntry.Create(
            tenantId,
            DateTime.UtcNow,
            $"Satış iadesi — Sipariş #{orderId.ToString()[..8]}, İade #{returnRequestId.ToString()[..8]}",
            $"RET-{returnRequestId.ToString()[..8]}");

        // BORÇ: 610 Satıştan İadeler (gelir azalışı)
        entry.AddLine(AccountingConstants.Account610SalesReturns, totalRefundAmount, 0, "610 Satıştan İadeler");

        // ALACAK: 120 Alıcılar (müşteri borcundan düşer)
        entry.AddLine(AccountingConstants.Account120Receivables, 0, totalRefundAmount, "120 Alıcılar — iade kesintisi");

        entry.Validate();
        entry.Post();

        await _journalRepo.AddAsync(entry, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "İade ters GL kaydı oluşturuldu — ReturnId={ReturnId}, Amount={Amount}",
            returnRequestId, totalRefundAmount);
    }
}
