using MesTech.Domain.Entities;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Bitrix24 CRM adapter — deal push, contact sync, catalog management.
/// Extends IIntegratorAdapter with Bitrix24-specific CRM operations.
/// SupportsStockUpdate = false (CRM-focused, not e-commerce stock).
/// </summary>
public interface IBitrix24Adapter : IIntegratorAdapter
{
    /// <summary>Push a MesTech Order as a Bitrix24 Deal (crm.deal.add + crm.deal.productrows.set).</summary>
    Task<Guid?> PushDealAsync(Order order, CancellationToken ct = default);

    /// <summary>Sync MesTech Customers ↔ Bitrix24 Contacts (crm.contact.list/add/update).</summary>
    Task<int> SyncContactsAsync(CancellationToken ct = default);

    /// <summary>Pull Bitrix24 catalog products (catalog.product.list).</summary>
    Task<IReadOnlyList<Product>> GetCatalogProductsAsync(CancellationToken ct = default);

    /// <summary>Update deal stage (crm.deal.update STAGE_ID).</summary>
    Task<bool> UpdateDealStageAsync(string externalDealId, string stageId, CancellationToken ct = default);

    /// <summary>Execute multiple Bitrix24 REST commands in a single HTTP call (batch method, max 50/request).</summary>
    Task<IReadOnlyList<string>> BatchExecuteAsync(IReadOnlyList<string> commands, CancellationToken ct = default);
}
