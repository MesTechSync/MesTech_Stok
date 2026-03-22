namespace MesTech.Domain.Enums;

/// <summary>
/// Yevmiye kaydinin kaynak belge turunu belirtir.
/// JournalEntry.SourceType alani icin kullanilir.
/// </summary>
public enum JournalSourceType
{
    Manual = 0,
    SalesInvoice = 1,
    SalesReturn = 2,
    PurchaseInvoice = 3,
    Commission = 4,
    ShippingCost = 5,
    OrderRevenue = 6,
    Reversal = 7,
    BankTransaction = 8,
    CashTransaction = 9
}
