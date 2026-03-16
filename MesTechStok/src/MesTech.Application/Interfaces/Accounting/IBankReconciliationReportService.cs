namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Banka mutabakat rapor servisi arayuzu.
/// Banka ekstre hareketleri ile muhasebe kayitlarini karsilastirir.
/// </summary>
public interface IBankReconciliationReportService
{
    /// <summary>
    /// Belirtilen tarih araligi icin banka mutabakat raporu olusturur.
    /// </summary>
    Task<BankReconciliationReportDto> GenerateReportAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);
}

/// <summary>
/// Banka mutabakat rapor sonucu.
/// </summary>
public class BankReconciliationReportDto
{
    public Guid TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Eslesen hareketler.
    /// </summary>
    public List<ReconciliationItemDto> MatchedItems { get; set; } = new();

    /// <summary>
    /// Bankada olup muhasebede olmayan hareketler.
    /// </summary>
    public List<ReconciliationItemDto> UnmatchedBankItems { get; set; } = new();

    /// <summary>
    /// Muhasebede olup bankada olmayan hareketler.
    /// </summary>
    public List<ReconciliationItemDto> UnmatchedAccountingItems { get; set; } = new();

    public decimal BankBalance => MatchedItems.Sum(i => i.Amount)
        + UnmatchedBankItems.Sum(i => i.Amount);

    public decimal AccountingBalance => MatchedItems.Sum(i => i.Amount)
        + UnmatchedAccountingItems.Sum(i => i.Amount);

    public decimal Difference => BankBalance - AccountingBalance;
}

/// <summary>
/// Mutabakat hareket satiri.
/// </summary>
public class ReconciliationItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Reference { get; set; }
    public string Source { get; set; } = string.Empty; // "Bank" or "Accounting"
}
