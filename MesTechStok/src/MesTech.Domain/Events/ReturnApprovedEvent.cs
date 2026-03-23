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
    IReadOnlyList<ReturnApprovedEvent.ReturnLineInfo> Lines,
    DateTime OccurredAt) : IDomainEvent
{
    public record ReturnLineInfo(
        Guid ProductId,
        string SKU,
        int Quantity,
        decimal UnitPrice);
}
