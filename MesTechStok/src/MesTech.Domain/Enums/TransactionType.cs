namespace MesTech.Domain.Enums;

/// <summary>
/// Cari hesap hareket tipleri.
/// </summary>
public enum TransactionType
{
    None = 0,

    // Müşteri tarafı
    SalesInvoice = 1,       // Satış faturası → Borç (debit)
    Collection = 2,         // Tahsilat → Alacak (credit)
    SalesReturn = 3,        // Satış iadesi → Alacak (credit)
    PlatformCommission = 4, // Platform komisyonu → Gider (debit)

    // Tedarikçi tarafı
    PurchaseInvoice = 10,   // Alış faturası → Borç (credit — tedarikçiye borçlanma)
    Payment = 11,           // Ödeme → Alacak (debit — tedarikçiye ödeme)
    PurchaseReturn = 12,    // Alış iadesi → Alacak (debit)

    // Ortak
    Adjustment = 20,        // Manuel düzeltme
    OpeningBalance = 21     // Açılış bakiyesi
}
