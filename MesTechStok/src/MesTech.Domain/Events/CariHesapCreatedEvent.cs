using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// Yeni cari hesap oluşturulduğunda tetiklenir.
/// Audit trail + ERP sync + bildirim zinciri.
/// </summary>
public record CariHesapCreatedEvent(
    Guid CariHesapId,
    Guid TenantId,
    string Name,
    CariHesapType Type,
    DateTime OccurredAt
) : IDomainEvent;
