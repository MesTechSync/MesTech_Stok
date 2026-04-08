using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// Cari hareket kaydedildiğinde tetiklenir.
/// Muhasebe GL kaydı + bakiye güncelleme + bildirim zinciri.
/// </summary>
public record CariHareketRecordedEvent(
    Guid CariHareketId,
    Guid CariHesapId,
    Guid TenantId,
    decimal Amount,
    CariDirection Direction,
    DateTime OccurredAt
) : IDomainEvent;
