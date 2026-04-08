using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// Kampanya oluşturulduğunda tetiklenir.
/// Handler: platform fiyat güncelleme, bildirim.
/// </summary>
public record CampaignCreatedEvent(
    Guid CampaignId,
    Guid TenantId,
    string Name,
    decimal DiscountPercent,
    PlatformType? PlatformType,
    DateTime StartDate,
    DateTime EndDate,
    DateTime OccurredAt
) : IDomainEvent;
