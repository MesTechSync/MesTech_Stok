using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// StockChangedEvent → ERPNext Stock Entry (Material Receipt/Issue).
/// </summary>
public interface IERPNextStockEntryHandler
{
    Task HandleAsync(Guid productId, string sku, Guid tenantId,
        int oldStock, int newStock, string reason, CancellationToken ct = default);
}

public sealed class ERPNextStockEntryHandler : IERPNextStockEntryHandler
{
    private readonly IErpBridgeService _erpBridge;
    private readonly ILogger<ERPNextStockEntryHandler> _logger;

    public ERPNextStockEntryHandler(IErpBridgeService erpBridge, ILogger<ERPNextStockEntryHandler> logger)
    {
        _erpBridge = erpBridge;
        _logger = logger;
    }

    public async Task HandleAsync(Guid productId, string sku, Guid tenantId,
        int oldStock, int newStock, string reason, CancellationToken ct = default)
    {
        var entryType = newStock > oldStock ? "Material Receipt" : "Material Issue";
        var quantity = Math.Abs(newStock - oldStock);

        _logger.LogInformation(
            "ERPNext StockEntry: SKU={SKU}, Type={Type}, Qty={Qty}, {Old}→{New}",
            sku, entryType, quantity, oldStock, newStock);

        await _erpBridge.PushStockEntryAsync(tenantId, productId, sku, entryType, quantity, reason, ct)
            .ConfigureAwait(false);
    }
}
