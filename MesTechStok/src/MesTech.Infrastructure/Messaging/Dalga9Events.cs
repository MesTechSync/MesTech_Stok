namespace MesTech.Infrastructure.Messaging;

/// <summary>
/// Dalga 9 — On Muhasebe & Kargo Genisletme Integration Events
/// E-Fatura, ERP entegrasyonu, eBay ve ilgili messenger olaylari.
/// </summary>

// ════════════════════════════════════════════════════════════════
// MesTech → MESA (Publish)
// ════════════════════════════════════════════════════════════════

/// <summary>E-Fatura GIB'e basariyla gonderildi — MESA OS WhatsApp bildirim tetikler.</summary>
public record EInvoiceSentIntegrationEvent(
    Guid InvoiceId,
    string EttnNo,
    string ProviderId,
    decimal TotalAmount,
    string Currency,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>E-Fatura iptal edildi — MESA OS muhasebe modülüne bildiri.</summary>
public record EInvoiceCancelledIntegrationEvent(
    Guid InvoiceId,
    string EttnNo,
    string Reason,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>ERP senkronizasyonu tamamlandi — Parasut, Logo vs entegrasyon kontrol.</summary>
public record ErpSyncCompletedIntegrationEvent(
    string ErpProvider,
    string EntityType,
    Guid EntityId,
    string? ErpRef,
    bool Success,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>eBay siparisi alindi — Kargo + muhasebe modüllerine bildir.</summary>
public record EbayOrderReceivedIntegrationEvent(
    string EbayOrderId,
    string BuyerUsername,
    decimal TotalAmount,
    string Currency,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>Kredi bakiyesi dusuk — E-Fatura ve SMS kredisi var mi kontrolü.</summary>
public record CreditBalanceLowIntegrationEvent(
    string ProviderId,
    int RemainingCredits,
    int ThresholdCredits,
    Guid TenantId,
    DateTime OccurredAt);

// ════════════════════════════════════════════════════════════════
// MESA → MesTech (Consume)
// ════════════════════════════════════════════════════════════════

/// <summary>MESA AI e-fatura taslagı olusturdu — muhasebe onayina gonder.</summary>
public record AiEInvoiceDraftGeneratedIntegrationEvent(
    Guid OrderId,
    string SuggestedEttnNo,
    decimal SuggestedTotal,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>MESA AI ERP uzlaştırmasi tamamladı — uyuşmazlıklar raporlanır.</summary>
public record AiErpReconciliationDoneIntegrationEvent(
    string ErpProvider,
    int ReconciledCount,
    int MismatchCount,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>MESA Bot e-fatura talep etti — WhatsApp/Telegram kanalı mekanizması.</summary>
public record BotEFaturaRequestedIntegrationEvent(
    string BotUserId,
    Guid? OrderId,
    string? BuyerVkn,
    Guid TenantId,
    DateTime OccurredAt);
