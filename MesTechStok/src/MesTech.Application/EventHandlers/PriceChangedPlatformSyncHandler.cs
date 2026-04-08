using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using IProductPlatformMappingRepository = MesTech.Domain.Interfaces.IProductPlatformMappingRepository;
using ISyncLogRepository = MesTech.Domain.Interfaces.ISyncLogRepository;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Fiyat değiştiğinde platforma sync tetikler.
/// PriceChangedEvent → ProductPlatformMapping → Adapter.PushPriceUpdateAsync → SyncLog.
/// </summary>
public interface IPriceChangedPlatformSyncHandler
{
    Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        decimal oldPrice, decimal newPrice,
        CancellationToken ct);
}

public sealed class PriceChangedPlatformSyncHandler : IPriceChangedPlatformSyncHandler
{
    private readonly IProductPlatformMappingRepository _mappingRepo;
    private readonly IAdapterFactory _adapterFactory;
    private readonly ISyncLogRepository _syncLogRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PriceChangedPlatformSyncHandler> _logger;

    public PriceChangedPlatformSyncHandler(
        IProductPlatformMappingRepository mappingRepo,
        IAdapterFactory adapterFactory,
        ISyncLogRepository syncLogRepo,
        IUnitOfWork uow,
        ILogger<PriceChangedPlatformSyncHandler> logger)
    {
        _mappingRepo = mappingRepo;
        _adapterFactory = adapterFactory;
        _syncLogRepo = syncLogRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        decimal oldPrice, decimal newPrice,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "PriceChanged → platform sync başlıyor. SKU={SKU}, Eski={Old}, Yeni={New}, TenantId={TenantId}",
            sku, oldPrice, newPrice, tenantId);

        var mappings = await _mappingRepo.GetByProductIdAsync(productId, ct).ConfigureAwait(false);
        var activeMappings = mappings.Where(m => m.IsEnabled).ToList();

        if (activeMappings.Count == 0)
        {
            _logger.LogDebug("SKU={SKU} aktif platform eşlemesi yok — price sync atlanıyor.", sku);
            return;
        }

        foreach (var mapping in activeMappings)
        {
            await SyncPriceToPlatformAsync(mapping, productId, tenantId, sku, newPrice, ct).ConfigureAwait(false);
        }

        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task SyncPriceToPlatformAsync(
        ProductPlatformMapping mapping,
        Guid productId,
        Guid tenantId,
        string sku,
        decimal newPrice,
        CancellationToken ct)
    {
        var platformCode = mapping.PlatformType.ToString();
        var adapter = _adapterFactory.Resolve(mapping.PlatformType);

        if (adapter == null)
        {
            _logger.LogWarning(
                "Platform adapter bulunamadı: {Platform} — SKU={SKU} price sync atlanıyor.",
                platformCode, sku);
            return;
        }

        if (!adapter.SupportsPriceUpdate)
        {
            _logger.LogDebug("Platform {Platform} fiyat güncelleme desteklemiyor — atlanıyor.", platformCode);
            return;
        }

        var syncLog = new SyncLog
        {
            TenantId = tenantId,
            PlatformCode = platformCode,
            Direction = SyncDirection.Push,
            EntityType = "Product.Price",
            EntityId = productId.ToString(),
            StartedAt = DateTime.UtcNow,
            ItemsProcessed = 0,
            CorrelationId = Guid.NewGuid().ToString("N")
        };

        try
        {
            var success = await adapter.PushPriceUpdateAsync(productId, newPrice, ct).ConfigureAwait(false);

            syncLog.IsSuccess = success;
            syncLog.SyncStatus = success ? SyncStatus.Synced : SyncStatus.Failed;
            syncLog.ItemsProcessed = success ? 1 : 0;
            syncLog.ItemsFailed = success ? 0 : 1;
            syncLog.CompletedAt = DateTime.UtcNow;

            if (success)
            {
                mapping.LastSyncDate = DateTime.UtcNow;
                mapping.SyncStatus = SyncStatus.Synced;
                await _mappingRepo.UpdateAsync(mapping, ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "PriceSync OK — {Platform} SKU={SKU} Price={Price}",
                    platformCode, sku, newPrice);
            }
            else
            {
                syncLog.ErrorMessage = "Adapter returned false";
                _logger.LogWarning(
                    "PriceSync FAIL — {Platform} SKU={SKU} adapter returned false.",
                    platformCode, sku);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            syncLog.IsSuccess = false;
            syncLog.SyncStatus = SyncStatus.Failed;
            syncLog.ItemsFailed = 1;
            syncLog.ErrorMessage = ex.Message;
            syncLog.CompletedAt = DateTime.UtcNow;

            _logger.LogError(ex,
                "PriceSync EXCEPTION — {Platform} SKU={SKU}: {Error}",
                platformCode, sku, ex.Message);
        }

        await _syncLogRepo.AddAsync(syncLog, ct).ConfigureAwait(false);
    }
}
