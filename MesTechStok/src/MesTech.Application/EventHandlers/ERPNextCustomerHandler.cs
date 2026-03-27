using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// CustomerCreatedEvent → ERPNext Customer CRM kayıt.
/// </summary>
public interface IERPNextCustomerHandler
{
    Task HandleAsync(Guid customerId, Guid tenantId, string customerName,
        string? email, string? phone, CancellationToken ct = default);
}

public sealed class ERPNextCustomerHandler : IERPNextCustomerHandler
{
    private readonly IErpBridgeService _erpBridge;
    private readonly ILogger<ERPNextCustomerHandler> _logger;

    public ERPNextCustomerHandler(IErpBridgeService erpBridge, ILogger<ERPNextCustomerHandler> logger)
    {
        _erpBridge = erpBridge;
        _logger = logger;
    }

    public async Task HandleAsync(Guid customerId, Guid tenantId, string customerName,
        string? email, string? phone, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ERPNext Customer: CustomerId={CustomerId}, Name={Name}",
            customerId, customerName);

        await _erpBridge.PushCustomerAsync(tenantId, customerId, customerName, email, phone, ct)
            .ConfigureAwait(false);
    }
}
