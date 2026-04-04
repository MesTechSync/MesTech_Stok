using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Fatura platformda gonderidiginde (Trendyol, HB vb.) firlatilir.
/// Handler: Platform fatura takip, musteri bilgilendirme.
/// </summary>
public record InvoicePlatformSentEvent(
    Guid InvoiceId,
    Guid TenantId,
    Guid OrderId,
    string PlatformInvoiceUrl,
    DateTime OccurredAt
) : IDomainEvent;
