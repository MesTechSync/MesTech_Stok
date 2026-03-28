using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Finance;

/// <summary>
/// Banka mutabakat rapor servisi.
/// Banka ekstre hareketleri ile muhasebe kayitlarini karsilastirir.
/// Eslesen, bankada olup muhasebede olmayan, muhasebede olup bankada olmayan
/// hareketleri raporlar.
/// </summary>
public sealed class BankReconciliationReportService : IBankReconciliationReportService
{
    /// <summary>
    /// Tutar esleme toleransi (kurus farklari icin).
    /// </summary>
    private const decimal AmountTolerance = 0.01m;

    private readonly IBankTransactionRepository _bankTxRepo;
    private readonly IJournalEntryRepository _journalRepo;
    private readonly IChartOfAccountsRepository _chartRepo;
    private readonly ILogger<BankReconciliationReportService> _logger;

    public BankReconciliationReportService(
        IBankTransactionRepository bankTxRepo,
        IJournalEntryRepository journalRepo,
        IChartOfAccountsRepository chartRepo,
        ILogger<BankReconciliationReportService> logger)
    {
        _bankTxRepo = bankTxRepo ?? throw new ArgumentNullException(nameof(bankTxRepo));
        _journalRepo = journalRepo ?? throw new ArgumentNullException(nameof(journalRepo));
        _chartRepo = chartRepo ?? throw new ArgumentNullException(nameof(chartRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<BankReconciliationReportDto> GenerateReportAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId bos olamaz.", nameof(tenantId));
        if (endDate < startDate)
            throw new ArgumentException("Bitis tarihi baslangic tarihinden once olamaz.", nameof(endDate));

        // Fetch unreconciled bank transactions
        var allBankTxs = await _bankTxRepo.GetUnreconciledAsync(tenantId, ct);
        var bankTxsInRange = allBankTxs
            .Where(tx => tx.TransactionDate >= startDate && tx.TransactionDate <= endDate)
            .ToList();

        // Fetch accounting journal entries for the period
        var journalEntries = await _journalRepo.GetByDateRangeAsync(tenantId, startDate, endDate, ct);

        // Get bank accounts (102.xx) to identify bank-related journal lines
        var allAccounts = await _chartRepo.GetAllAsync(tenantId, isActive: true, ct: ct);
        var bankAccountIds = allAccounts
            .Where(a => a.Code.StartsWith("102", StringComparison.Ordinal))
            .Select(a => a.Id)
            .ToHashSet();

        // Extract bank-related journal lines
        var accountingItems = journalEntries
            .Where(e => e.IsPosted)
            .SelectMany(e => e.Lines)
            .Where(l => bankAccountIds.Contains(l.AccountId))
            .Select(l => new ReconciliationItemDto
            {
                Description = l.Description ?? l.JournalEntry?.Description ?? string.Empty,
                Amount = l.Debit - l.Credit,
                TransactionDate = l.JournalEntry?.EntryDate ?? l.CreatedAt,
                Reference = l.JournalEntry?.ReferenceNumber,
                Source = "Accounting"
            }).ToList();

        // Convert bank transactions to reconciliation items
        var bankItems = bankTxsInRange.Select(tx => new ReconciliationItemDto
        {
            Description = tx.Description,
            Amount = tx.Amount,
            TransactionDate = tx.TransactionDate,
            Reference = tx.ReferenceNumber,
            Source = "Bank"
        }).ToList();

        // Match items by amount and date (same day)
        var matched = new List<ReconciliationItemDto>();
        var unmatchedBank = new List<ReconciliationItemDto>(bankItems);
        var unmatchedAccounting = new List<ReconciliationItemDto>(accountingItems);

        foreach (var bankItem in bankItems.ToList())
        {
            var match = unmatchedAccounting.FirstOrDefault(a =>
                Math.Abs(a.Amount - bankItem.Amount) <= AmountTolerance
                && a.TransactionDate.Date == bankItem.TransactionDate.Date);

            if (match is not null)
            {
                matched.Add(bankItem);
                unmatchedBank.Remove(bankItem);
                unmatchedAccounting.Remove(match);
            }
        }

        var report = new BankReconciliationReportDto
        {
            TenantId = tenantId,
            StartDate = startDate,
            EndDate = endDate,
            MatchedItems = matched,
            UnmatchedBankItems = unmatchedBank,
            UnmatchedAccountingItems = unmatchedAccounting
        };

        _logger.LogInformation(
            "Banka mutabakat raporu — Tenant={TenantId}, {Start:d}~{End:d}, " +
            "Eslesen={Matched}, Bankada={UnmBank}, Muhasebede={UnmAcc}",
            tenantId, startDate, endDate,
            matched.Count, unmatchedBank.Count, unmatchedAccounting.Count);

        return report;
    }
}
