namespace MesTech.Domain.Enums;

/// <summary>
/// Stok hareket türleri — birleştirilmiş enum yapısı.
/// </summary>
public enum StockMovementType
{
    // === GİRİŞ HAREKETLERİ ===
    StockIn = 1,
    Purchase = 10,
    BarcodeReceive = 11,
    Production = 12,
    CustomerReturn = 13,
    Found = 14,
    PlatformReturn = 15,

    // === ÇIKIŞ HAREKETLERİ ===
    StockOut = 2,
    Sale = 20,
    BarcodeSale = 21,
    Consumption = 22,
    Loss = 23,
    PlatformSale = 24,

    // === DÜZELTME & TRANSFER ===
    Adjustment = 3,
    Transfer = 30,

    // === ENTEGRASYON SYNC ===
    PlatformSync = 6,
    OpenCartSync = 60,
    TrendyolSync = 61,
    MarketplaceSync = 62,

    // Uyumluluk (deprecated — geçiş sürecinde)
    [Obsolete("Use StockIn instead")]
    In = 7,
    [Obsolete("Use StockOut instead")]
    Out = 8,
    [Obsolete("Use CustomerReturn instead")]
    Return = 13
}
