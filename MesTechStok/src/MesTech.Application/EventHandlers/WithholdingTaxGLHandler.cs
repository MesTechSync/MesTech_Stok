using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Stopaj (tevkifat) kesildiğinde GL kaydı oluşturur (Zincir 7).
/// Pazaryeri %1 gelir vergisi stopajı keser → 193 Peşin Ödenen Vergiler hesabına kaydedilir.
/// Yılsonunda 193 hesap, 371 Dönem Karı Vergisi ile mahsup edilir.
///
/// Çift kayıt: BORÇ 193 Peşin Ödenen Vergiler / ALACAK 120 Alıcılar
/// (stopaj hakedisten kesildiği için alacak azalır)
/// </summary>
public interface IWithholdingTaxGLHandler
{
    Task HandleAsync(
        Guid withholdingId, Guid tenantId,
        decimal taxExclusiveAmount, decimal rate, decimal withholdingAmount,
        string taxType, CancellationToken ct);
}

public sealed class WithholdingTaxGLHandler : IWithholdingTaxGLHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalEntryRepository _journalRepo;
    private readonly ILogger<WithholdingTaxGLHandler> _logger;

    public WithholdingTaxGLHandler(
        IUnitOfWork unitOfWork,
        IJournalEntryRepository journalRepo,
        ILogger<WithholdingTaxGLHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _journalRepo = journalRepo;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid withholdingId, Guid tenantId,
        decimal taxExclusiveAmount, decimal rate, decimal withholdingAmount,
        string taxType, CancellationToken ct)
    {
        if (withholdingAmount <= 0)
        {
            _logger.LogDebug("Stopaj tutarı 0 — GL atlanıyor. WithholdingId={Id}", withholdingId);
            return;
        }

        var refNumber = $"WHT-{withholdingId.ToString("N")[..12]}";

        // Idempotency guard
        if (await _journalRepo.ExistsByReferenceAsync(tenantId, refNumber, ct))
        {
            _logger.LogWarning("Duplicate stopaj GL — ref {Ref} zaten var, atlanıyor", refNumber);
            return;
        }

        _logger.LogInformation(
            "Stopaj GL oluşturuluyor — Matrah={Base}, Oran=%{Rate}, Tutar={Amount}, Tip={Type}",
            taxExclusiveAmount, rate * 100, withholdingAmount, taxType);

        var entry = JournalEntry.Create(
            tenantId,
            DateTime.UtcNow,
            $"Stopaj kesintisi — {taxType} %{rate * 100:F1}",
            refNumber);

        // BORÇ: 193 Peşin Ödenen Vergiler (yılsonunda mahsup edilecek)
        entry.AddLine(AccountingConstants.Account193PrepaidTax, withholdingAmount, 0,
            $"193 Peşin Öd.Vergi — {taxType} %{rate * 100:F1}");

        // ALACAK: 120 Alıcılar (stopaj hakedisten kesilir)
        entry.AddLine(AccountingConstants.Account120Receivables, 0, withholdingAmount,
            $"120 Alıcılar — stopaj kesintisi");

        entry.Validate();
        entry.Post();

        await _journalRepo.AddAsync(entry, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Stopaj GL tamamlandı — EntryId={EntryId}, Tutar={Amount}, Tip={Type}",
            entry.Id, withholdingAmount, taxType);
    }
}
