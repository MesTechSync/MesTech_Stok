namespace MesTech.Infrastructure.Integration.Settlement.Mapping;

/// <summary>
/// Etsy Payments API ledger entry model.
/// Amounts are in CENTS (divide by 100 for currency units).
/// Types: sale, fee, refund, deposit, recoupment, other.
/// </summary>
internal sealed class EtsySettlementLine
{
    public long LedgerEntryId { get; set; }
    public string EntryType { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public long? ReferenceId { get; set; }
    public long? CreateDate { get; set; }
}
