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
    private readonly ILogger<CommissionChargedGLHandler> _logger;

    public CommissionChargedGLHandler(
        IUnitOfWork unitOfWork,
        ILogger<CommissionChargedGLHandler> logger)
    {
        _unitOfWork = unitOfWork;
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

        _logger.LogInformation(
            "Komisyon GL kaydi — OrderId={OrderId}, Platform={Platform}, Amount={Amount}, Rate={Rate}%",
            orderId, platform, commissionAmount, commissionRate * 100);

        var entry = JournalEntry.Create(
            tenantId,
            DateTime.UtcNow,
            $"Platform komisyonu — {platform} Siparis #{orderId.ToString()[..8]}",
            $"COM-{orderId.ToString()[..8]}");

        // BORC: 760.02 Komisyon Giderleri
        entry.AddLine(AccountingConstants.Account760MarketingExpenses, commissionAmount, 0, $"760.02 Komisyon — {platform} %{commissionRate * 100:F1}");

        // ALACAK: 120 Alicilar (hakedisten kesilir)
        entry.AddLine(AccountingConstants.Account120Receivables, 0, commissionAmount, $"120 Alicilar — {platform} komisyon kesintisi");

        entry.Validate();
        entry.Post();

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Komisyon GL tamamlandi — Platform={Platform}, Amount={Amount}, EntryId={EntryId}",
            platform, commissionAmount, entry.Id);
    }
}
