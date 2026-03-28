using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Stok değiştiğinde platforma sync tetikler (Zincir 9).
/// StockChangedEvent → Platform adapter'lara stok güncelleme bildirimi.
/// Gerçek API çağrısı Infrastructure adapter'larda yapılır.
/// Bu handler sadece log + downstream event dispatch sorumluluğundadır.
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
    private readonly ILogger<StockChangedPlatformSyncHandler> _logger;

    public StockChangedPlatformSyncHandler(
        ILogger<StockChangedPlatformSyncHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(
        Guid productId, Guid tenantId, string sku,
        int previousQuantity, int newQuantity,
        StockMovementType movementType,
        CancellationToken ct)
    {
        var delta = newQuantity - previousQuantity;
        _logger.LogInformation(
            "StockChanged → platform sync tetikleniyor. SKU={SKU}, Önceki={Prev}, Yeni={New}, Delta={Delta}, MovementType={MovementType}, TenantId={TenantId}",
            sku, previousQuantity, newQuantity, delta, movementType, tenantId);

        if (newQuantity == 0)
        {
            _logger.LogWarning(
                "STOK SIFIR! SKU={SKU} — ZeroStockDetectedEvent ayrıca tetiklenmelidir. ProductId={ProductId}",
                sku, productId);
        }

        // Platform sync gerçek implementasyonu Infrastructure katmanında
        // MassTransit consumer veya Hangfire job olarak çalışır.
        // Bu handler Application katmanında orchestration/logging sorumluluğundadır.
        return Task.CompletedTask;
    }
}
