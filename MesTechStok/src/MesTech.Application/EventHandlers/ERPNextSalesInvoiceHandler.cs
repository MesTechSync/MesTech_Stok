using MesTech.Application.Interfaces;
using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// OrderCompletedEvent → ERPNext Sales Invoice oluştur.
/// IErpBridgeService üzerinden push.
/// </summary>
public interface IERPNextSalesInvoiceHandler
{
    Task HandleAsync(Guid orderId, Guid tenantId, string orderNumber,
        decimal totalAmount, string? customerName, CancellationToken ct = default);
}

public sealed class ERPNextSalesInvoiceHandler : IERPNextSalesInvoiceHandler
{
    private readonly IErpBridgeService _erpBridge;
    private readonly ILogger<ERPNextSalesInvoiceHandler> _logger;

    public ERPNextSalesInvoiceHandler(IErpBridgeService erpBridge, ILogger<ERPNextSalesInvoiceHandler> logger)
    {
        _erpBridge = erpBridge;
        _logger = logger;
    }

    public async Task HandleAsync(Guid orderId, Guid tenantId, string orderNumber,
        decimal totalAmount, string? customerName, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ERPNext SalesInvoice: OrderId={OrderId}, OrderNumber={OrderNumber}, Total={Total}",
            orderId, orderNumber, totalAmount);

        await _erpBridge.PushSalesInvoiceAsync(tenantId, orderId, orderNumber, totalAmount, customerName, ct)
            .ConfigureAwait(false);
    }
}
