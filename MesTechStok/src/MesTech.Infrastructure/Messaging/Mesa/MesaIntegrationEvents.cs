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

/// <summary>Fatura GIB'e basariyla gonderildi — MESA OS WhatsApp + e-posta bildirim tetikler.</summary>
public record MesaInvoiceGeneratedEvent(
    Guid InvoiceId,
    Guid OrderId,
    string InvoiceNumber,
    string InvoiceType,
    string? CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    decimal GrandTotal,
    string? PdfUrl,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>Fatura iptal edildi.</summary>
public record MesaInvoiceCancelledEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    string? CancelReason,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>Iade talebi olusturuldu — MESA OS musteri bildirim tetikler.</summary>
public record MesaReturnCreatedEvent(
    Guid ReturnRequestId,
    Guid OrderId,
    string PlatformCode,
    string? CustomerName,
    string? CustomerPhone,
    string ReturnReason,
    int ItemCount,
    decimal TotalAmount,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>Iade tamamlandi veya reddedildi.</summary>
public record MesaReturnResolvedEvent(
    Guid ReturnRequestId,
    Guid OrderId,
    string Resolution,
    decimal RefundAmount,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>Buybox kaybedildi — MESA OS acil fiyat onerisi hesaplar.</summary>
public record MesaBuyboxLostEvent(
    Guid ProductId,
    string SKU,
    decimal CurrentPrice,
    decimal CompetitorPrice,
    string CompetitorName,
    decimal PriceDifference,
    Guid TenantId,
    DateTime OccurredAt);

/// <summary>Tedarikci feed sync tamamlandi.</summary>
public record MesaSupplierFeedSyncedEvent(
    Guid SupplierId,
    string SupplierName,
    string FeedFormat,
    int ProductsTotal,
    int ProductsNew,
    int ProductsUpdated,
    int ProductsDeactivated,
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

/// <summary>MESA AI buybox bazli fiyat optimizasyonu hesaplandi.</summary>
public record MesaAiPriceOptimizedEvent(
    Guid ProductId,
    string SKU,
    decimal RecommendedPrice,
    decimal MinPrice,
    decimal MaxPrice,
    decimal? CompetitorMinPrice,
    double Confidence,
    string? Reasoning,
    DateTime GeneratedAt);

/// <summary>MESA AI stok tahmini hazir.</summary>
public record MesaAiStockPredictedEvent(
    Guid ProductId,
    string SKU,
    int PredictedDemand7d,
    int PredictedDemand14d,
    int PredictedDemand30d,
    int DaysUntilStockout,
    int ReorderSuggestion,
    double Confidence,
    string? Reasoning,
    DateTime GeneratedAt);

/// <summary>Musteri WhatsApp'tan fatura istedi.</summary>
public record MesaBotInvoiceRequestedEvent(
    string CustomerPhone,
    string OrderNumber,
    string RequestChannel,
    DateTime RequestedAt);

/// <summary>Musteri WhatsApp'tan iade istedi.</summary>
public record MesaBotReturnRequestedEvent(
    string CustomerPhone,
    string OrderNumber,
    string? ReturnReason,
    string RequestChannel,
    DateTime RequestedAt);
