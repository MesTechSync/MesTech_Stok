using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Fiyat degistiginde PriceHistory kaydini olusturur.
/// PriceChangedEvent -> PriceHistory entity kaydi.
/// </summary>
public interface IPriceChangedHistoryRecordHandler
{
    Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        decimal oldPrice, decimal newPrice,
        string? changedBy, string? changeReason,
        CancellationToken ct);
}

public sealed class PriceChangedHistoryRecordHandler : IPriceChangedHistoryRecordHandler
{
    private readonly IPriceHistoryRepository _priceHistoryRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PriceChangedHistoryRecordHandler> _logger;

    public PriceChangedHistoryRecordHandler(
        IPriceHistoryRepository priceHistoryRepo,
        IUnitOfWork uow,
        ILogger<PriceChangedHistoryRecordHandler> logger)
    {
        _priceHistoryRepo = priceHistoryRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        decimal oldPrice, decimal newPrice,
        string? changedBy, string? changeReason,
        CancellationToken ct)
    {
        var record = new PriceHistory
        {
            TenantId = tenantId,
            ProductId = productId,
            OldPrice = oldPrice,
            NewPrice = newPrice,
            ChangedBy = changedBy ?? MesTech.Domain.Constants.DomainConstants.SystemUserName,
            ChangeReason = changeReason ?? "PriceChangedEvent",
            ChangedAt = DateTime.UtcNow
        };

        await _priceHistoryRepo.AddAsync(record, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "PriceHistory recorded — SKU={SKU}, {OldPrice} -> {NewPrice} ({Percent}%)",
            sku, oldPrice, newPrice, record.PriceChangePercent);
    }
}
