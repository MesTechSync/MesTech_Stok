using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Satış fiyatı alış fiyatının altına düştüğünde fırlatılır.
/// Handler: Bildirim gönder (Telegram/Email), Dashboard'da kırmızı badge.
/// </summary>
public record PriceLossDetectedEvent(
    Guid ProductId,
    Guid TenantId,
    string SKU,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal LossPerUnit,
    DateTime OccurredAt) : IDomainEvent;
