using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Events;

/// <summary>
/// Iade talebi onaylandiginda firlatilir.
/// Handler: Stok geri ekle (Zincir 5), ters GL kaydi (Zincir 4).
/// </summary>
public record ReturnApprovedEvent(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid TenantId,
    IReadOnlyList<ReturnLineInfoEvent> Lines,
    DateTime OccurredAt) : IDomainEvent;

/// <summary>
/// Iade kalem bilgisi — ReturnApprovedEvent icin yardimci record.
/// </summary>
public record ReturnLineInfoEvent(
    Guid ProductId,
    string SKU,
    int Quantity,
    decimal UnitPrice);
