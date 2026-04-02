using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Overselling girişimi tespit edildiğinde çalışır.
/// Zincir 15 kalbi: InsufficientStock → Log + Audit trail.
/// Monitoring dashboardları bu eventi yakalar.
/// </summary>
public interface IOversellingAttemptedEventHandler
{
    Task HandleAsync(
        Guid productId, string sku, Guid tenantId,
        int availableStock, int requestedQuantity, string? orderNumber,
        CancellationToken ct);
}

public sealed class OversellingAttemptedEventHandler : IOversellingAttemptedEventHandler
{
    private readonly IProductRepository _productRepo;
    private readonly ILogger<OversellingAttemptedEventHandler> _logger;

    public OversellingAttemptedEventHandler(
        IProductRepository productRepo,
        ILogger<OversellingAttemptedEventHandler> logger)
    {
        _productRepo = productRepo;
        _logger = logger;
    }

    public Task HandleAsync(
        Guid productId, string sku, Guid tenantId,
        int availableStock, int requestedQuantity, string? orderNumber,
        CancellationToken ct)
    {
        _logger.LogCritical(
            "OVERSELLING_ATTEMPTED — SKU={SKU}, Mevcut={Available}, İstenen={Requested}, " +
            "Fark={Deficit}, Order={OrderNumber}, TenantId={TenantId}, ProductId={ProductId}",
            sku, availableStock, requestedQuantity,
            requestedQuantity - availableStock, orderNumber, tenantId, productId);

        return Task.CompletedTask;
    }
}
