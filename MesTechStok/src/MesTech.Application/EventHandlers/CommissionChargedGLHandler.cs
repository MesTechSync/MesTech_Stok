using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Platform komisyonu kesildiginde GL gider kaydi olusturur (Zincir 6).
/// Cift kayit: BORC 760.02 Komisyon Giderleri, ALACAK 120 Alicilar (hakedisten duser).
/// </summary>
public interface ICommissionChargedGLHandler
{
    Task HandleAsync(
        Guid orderId, Guid tenantId, PlatformType platform,
        decimal commissionAmount, decimal commissionRate,
        CancellationToken ct);
}

public sealed class CommissionChargedGLHandler : ICommissionChargedGLHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalEntryRepository _journalRepo;
    private readonly ILogger<CommissionChargedGLHandler> _logger;

    public CommissionChargedGLHandler(
        IUnitOfWork unitOfWork,
        IJournalEntryRepository journalRepo,
        ILogger<CommissionChargedGLHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _journalRepo = journalRepo;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid orderId, Guid tenantId, PlatformType platform,
        decimal commissionAmount, decimal commissionRate,
        CancellationToken ct)
    {
        if (commissionAmount <= 0)
        {
            _logger.LogDebug("Komisyon tutari 0 — GL kaydi atlanıyor. OrderId={OrderId}", orderId);
            return;
        }

        var refNumber = $"COM-{orderId.ToString()[..8]}";
        if (await _journalRepo.ExistsByReferenceAsync(tenantId, refNumber, ct))
        {
            _logger.LogWarning("Duplicate komisyon GL — ref {Ref} zaten var, atlanıyor", refNumber);
            return;
        }

        _logger.LogInformation(
            "Komisyon GL kaydi — OrderId={OrderId}, Platform={Platform}, Amount={Amount}, Rate={Rate}%",
            orderId, platform, commissionAmount, commissionRate * 100);

        var entry = JournalEntry.Create(
            tenantId,
            DateTime.UtcNow,
            $"Platform komisyonu — {platform} Siparis #{orderId.ToString()[..8]}",
            $"COM-{orderId.ToString()[..8]}");

        // Komisyon faturası KDV'si (%20) — 191 İndirilecek KDV olarak mahsup edilir
        var commissionKdv = Math.Round(commissionAmount * 0.20m, 2);
        var totalWithKdv = commissionAmount + commissionKdv;

        // BORC: 760.02 Komisyon Giderleri (KDV hariç komisyon tutarı)
        entry.AddLine(AccountingConstants.Account760MarketingExpenses, commissionAmount, 0,
            $"760.02 Komisyon — {platform} %{commissionRate * 100:F1}");

        // BORC: 191 İndirilecek KDV (komisyon faturası KDV'si — mahsup edilecek)
        if (commissionKdv > 0)
            entry.AddLine(AccountingConstants.Account191VatReceivable, commissionKdv, 0,
                $"191 İnd.KDV — {platform} komisyon KDV");

        // ALACAK: 120 Alıcılar (komisyon + KDV toplam hakedisten kesilir)
        entry.AddLine(AccountingConstants.Account120Receivables, 0, totalWithKdv,
            $"120 Alıcılar — {platform} komisyon+KDV kesintisi");

        entry.Validate();
        entry.Post();

        await _journalRepo.AddAsync(entry, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Komisyon GL tamamlandi — Platform={Platform}, Amount={Amount}, EntryId={EntryId}",
            platform, commissionAmount, entry.Id);
    }
}
