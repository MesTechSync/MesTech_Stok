namespace MesTech.Domain.Accounting.Enums;

/// <summary>
/// Hesap kesimi (settlement) durumu.
/// </summary>
public enum SettlementStatus
{
    Imported = 0,
    Reconciled = 1,
    Partial = 2,
    Disputed = 3
}
