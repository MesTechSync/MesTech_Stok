using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Stok sıfıra düştüğünde fırlatılır.
/// Handler: Platformlarda ürünü pasife al (Zincir 8), bildirim gönder.
/// </summary>
public record ZeroStockDetectedEvent(
    Guid ProductId,
    Guid TenantId,
    string SKU,
    int PreviousStock,
    DateTime OccurredAt) : IDomainEvent;
