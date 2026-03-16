namespace MesTech.Domain.Accounting.Enums;

/// <summary>
/// Muhasebe belgesi turu.
/// </summary>
public enum DocumentType
{
    Invoice = 0,
    Receipt = 1,
    BankStatement = 2,
    Settlement = 3,
    Contract = 4,
    PurchaseInvoice = 5,
    SalesInvoice = 6,
    Other = 99
}
