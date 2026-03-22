using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Fatura ERP'ye aktarilmak uzere olusturuldugunda firlatilir.
/// IERPSyncHandler bu event'i dinleyerek hedef ERP'ye senkronizasyon baslatir.
/// </summary>
public record InvoiceGeneratedForERPEvent(
    Guid InvoiceId,
    Guid TenantId,
    string InvoiceNumber,
    decimal TotalAmount,
    string TargetERP,
    DateTime OccurredAt
) : IDomainEvent;
