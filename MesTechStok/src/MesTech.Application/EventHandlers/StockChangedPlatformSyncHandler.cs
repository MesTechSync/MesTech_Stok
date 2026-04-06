using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using IProductPlatformMappingRepository = MesTech.Domain.Interfaces.IProductPlatformMappingRepository;
using ISyncLogRepository = MesTech.Domain.Interfaces.ISyncLogRepository;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Stok değiştiğinde platforma sync tetikler (Zincir 9).
/// StockChangedEvent → ProductPlatformMapping → Adapter.PushStockUpdateAsync → SyncLog.
/// </summary>
public interface IStockChangedPlatformSyncHandler
{
    Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        int previousQuantity, int newQuantity,
        StockMovementType movementType,
        CancellationToken ct);
}

public sealed class StockChangedPlatformSyncHandler : IStockChangedPlatformSyncHandler
{
    private readonly IProductPlatformMappingRepository _mappingRepo;
    private readonly IAdapterFactory _adapterFactory;
    private readonly ISyncLogRepository _syncLogRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<StockChangedPlatformSyncHandler> _logger;

    public StockChangedPlatformSyncHandler(
        IProductPlatformMappingRepository mappingRepo,
        IAdapterFactory adapterFactory,
        ISyncLogRepository syncLogRepo,
        IUnitOfWork uow,
        ILogger<StockChangedPlatformSyncHandler> logger)
    {
        _mappingRepo = mappingRepo;
        _adapterFactory = adapterFactory;
        _syncLogRepo = syncLogRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        int previousQuantity, int newQuantity,
        StockMovementType movementType,
        CancellationToken ct)
    {
        var delta = newQuantity - previousQuantity;
        _logger.LogInformation(
            "StockChanged → platform sync başlıyor. SKU={SKU}, Önceki={Prev}, Yeni={New}, Delta={Delta}, TenantId={TenantId}",
            sku, previousQuantity, newQuantity, delta, tenantId);

        if (newQuantity == 0)
        {
            _logger.LogWarning(
                "STOK SIFIR! SKU={SKU} — platform'larda stok 0'a çekilecek. ProductId={ProductId}",
                sku, productId);
        }

        var mappings = await _mappingRepo.GetByProductIdAsync(productId, ct).ConfigureAwait(false);
        if (mappings.Count == 0)
        {
            _logger.LogDebug("SKU={SKU} hiçbir platformda eşlenmemiş — sync atlanıyor.", sku);
            return;
        }

        var activeMappings = mappings.Where(m => m.IsEnabled).ToList();
        if (activeMappings.Count == 0)
        {
            _logger.LogDebug("SKU={SKU} eşlemeleri var ama hepsi devre dışı — sync atlanıyor.", sku);
            return;
        }

        foreach (var mapping in activeMappings)
        {
            await SyncToPlatformAsync(mapping, productId, tenantId, sku, newQuantity, ct).ConfigureAwait(false);
        }

        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task SyncToPlatformAsync(
        ProductPlatformMapping mapping,
        Guid productId,
        Guid tenantId,
        string sku,
        int newQuantity,
        CancellationToken ct)
    {
        var platformCode = mapping.PlatformType.ToString();
        var adapter = _adapterFactory.Resolve(mapping.PlatformType);

        if (adapter == null)
        {
            _logger.LogWarning(
                "Platform adapter bulunamadı: {Platform} — SKU={SKU} sync atlanıyor.",
                platformCode, sku);
            return;
        }

        if (!adapter.SupportsStockUpdate)
        {
            _logger.LogDebug("Platform {Platform} stok güncelleme desteklemiyor — atlanıyor.", platformCode);
            return;
        }

        var syncLog = new SyncLog
        {
            TenantId = tenantId,
            PlatformCode = platformCode,
            Direction = SyncDirection.Push,
            EntityType = "Product.Stock",
            EntityId = productId.ToString(),
            StartedAt = DateTime.UtcNow,
            ItemsProcessed = 0,
            CorrelationId = Guid.NewGuid().ToString("N")
        };

        try
        {
            var success = await adapter.PushStockUpdateAsync(productId, newQuantity, ct).ConfigureAwait(false);

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
                    "StockSync OK — {Platform} SKU={SKU} Stock={Stock}",
                    platformCode, sku, newQuantity);
            }
            else
            {
                syncLog.ErrorMessage = "Adapter returned false";
                _logger.LogWarning(
                    "StockSync FAIL — {Platform} SKU={SKU} adapter returned false.",
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
                "StockSync EXCEPTION — {Platform} SKU={SKU}: {Error}",
                platformCode, sku, ex.Message);
        }

        await _syncLogRepo.AddAsync(syncLog, ct).ConfigureAwait(false);
    }
}
