namespace MesTech.Application.Interfaces;

/// <summary>
/// ERP sistemi köprü servisi — domain event'leri ERPNext/Parasut/Logo'ya push eder.
/// Her ERP adapter bu interface'i implemente eder.
/// </summary>
public interface IErpBridgeService
{
    Task PushSalesInvoiceAsync(Guid tenantId, Guid orderId, string orderNumber,
        decimal totalAmount, string? customerName, CancellationToken ct = default);

    Task PushStockEntryAsync(Guid tenantId, Guid productId, string sku,
        string entryType, int quantity, string reason, CancellationToken ct = default);

    Task PushCustomerAsync(Guid tenantId, Guid customerId, string customerName,
        string? email, string? phone, CancellationToken ct = default);
}
