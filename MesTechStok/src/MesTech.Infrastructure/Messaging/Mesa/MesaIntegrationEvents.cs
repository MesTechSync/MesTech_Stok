namespace MesTech.Infrastructure.Messaging.Mesa;

// ══════════════════════════════════════════════════════════════
// MesTech → MESA OS yonunde publish edilen integration event'ler
// Exchange prefix: mestech.mesa.*
// ══════════════════════════════════════════════════════════════

/// <summary>Yeni urun olusturuldu — MESA OS AI icerik uretimi tetikleyebilir.</summary>
public record MesaProductCreatedEvent(
    Guid ProductId,
    string SKU,
    string Name,
    string? Category,
    decimal SalePrice,
    List<string>? ImageUrls,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>Stok kritik seviyeye dustu — MESA OS stok tahmini + Telegram alarm tetikler.</summary>
public record MesaStockLowEvent(
    Guid ProductId,
    string SKU,
    int CurrentStock,
    int MinimumStock,
    int? ReorderSuggestion,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>Yeni siparis alindi — MESA OS WhatsApp bildirim tetikler.</summary>
public record MesaOrderReceivedEvent(
    Guid OrderId,
    string PlatformCode,
    string PlatformOrderId,
    decimal TotalAmount,
    string? CustomerPhone,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>Fiyat degisti — MESA OS fiyat optimizasyonu analiz tetikler.</summary>
public record MesaPriceChangedEvent(
    Guid ProductId,
    string SKU,
    decimal OldPrice,
    decimal NewPrice,
    Guid TenantId,
    DateTime OccurredAt);

// ══════════════════════════════════════════════════════════════
// MESA OS → MesTech yonunde consume edilen integration event'ler
// Exchange prefix: mesa.*
// ══════════════════════════════════════════════════════════════

/// <summary>MESA AI icerik uretimi tamamlandi.</summary>
public record MesaAiContentGeneratedEvent(
    Guid ProductId,
    string SKU,
    string GeneratedContent,
    Dictionary<string, string>? Metadata,
    string AiProvider,
    DateTime GeneratedAt);

/// <summary>MESA AI fiyat onerisi uretildi.</summary>
public record MesaAiPriceRecommendedEvent(
    Guid ProductId,
    string SKU,
    decimal RecommendedPrice,
    decimal MinPrice,
    decimal MaxPrice,
    string? Reasoning,
    DateTime GeneratedAt);

/// <summary>MESA Bot bildirim gonderim durumu.</summary>
public record MesaBotNotificationSentEvent(
    string Channel,
    string Recipient,
    bool Success,
    string? ErrorMessage,
    DateTime SentAt);
