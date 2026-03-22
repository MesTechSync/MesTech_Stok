using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// 48 saat geçmiş ama hâlâ gönderilmemiş sipariş tespit edildiğinde fırlatılır.
/// Handler: Satıcıya bildirim gönder, Dashboard'da uyarı göster.
/// </summary>
public record StaleOrderDetectedEvent(
    Guid OrderId,
    Guid TenantId,
    string OrderNumber,
    PlatformType? Platform,
    TimeSpan ElapsedSince,
    DateTime OccurredAt) : IDomainEvent;
