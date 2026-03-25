using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Bilanco dogrulama servisi.
/// Muhasebe temel denklemi: Assets == Liabilities + Equity.
/// Revenue ve Expense hesaplari donem net kari olarak Equity'ye dahil edilir.
///
/// Turkiye Tekduzen Hesap Plani (THP):
///   1xx = Varliklar (Asset)
///   2xx-3xx = Borclar (Liability)
///   5xx = Ozkaynaklar (Equity)
///   6xx = Gelirler (Revenue) -> Equity'ye net kar olarak yansir
///   7xx = Giderler (Expense) -> Equity'den cikarilir
///
/// Dogrulama adimi:
///   TotalAssets == TotalLiabilities + TotalEquity + (Revenue - Expense)
/// </summary>
public sealed class BalanceSheetValidationService : IBalanceSheetValidationService
{
    /// <summary>
    /// Kurus altindaki farklar icin tolerans.
    /// </summary>
    private const decimal Tolerance = 0.01m;

    /// <inheritdoc />
    public Task<BalanceSheetValidationResult> ValidateAsync(
        Guid tenantId,
        DateTime asOfDate,
        CancellationToken ct = default)
    {
        // Domain servisi — veri erisimi Application katmaninda saglanir.
        throw new InvalidOperationException(
            "Use ValidateFromData for direct validation. " +
            "For async repository access, use the Application layer handler.");
    }

    /// <summary>
    /// Hesap plani ve onaylanmis yevmiye satirlari uzerinden bilanco dogrulamasi yapar.
    /// Application katmanindaki handler repository'den veriyi alir ve bu metoda gonderir.
    /// </summary>
    /// <param name="accounts">Aktif hesap plani kayitlari.</param>
    /// <param name="postedLines">Onaylanmis yevmiye satirlari (asOfDate'e kadar).</param>
    /// <returns>Bilanco dogrulama sonucu.</returns>
    public BalanceSheetValidationResult ValidateFromData(
        IReadOnlyList<ChartOfAccounts> accounts,
        IReadOnlyList<JournalLine> postedLines)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(postedLines);

        var errors = new List<string>();
        var totals = CalculateTotals(accounts, postedLines, errors);

        // Net income flows into equity
        var netIncome = totals.Revenue - totals.Expenses;
        var adjustedEquity = totals.Equity + netIncome;

        var difference = totals.Assets - (totals.Liabilities + adjustedEquity);
        var isBalanced = Math.Abs(difference) <= Tolerance;

        if (!isBalanced)
        {
            errors.Add(
                $"Bilanco dengesiz: Varliklar={totals.Assets:N2} TL, " +
                $"Borclar={totals.Liabilities:N2} TL, " +
                $"Ozkaynaklar={adjustedEquity:N2} TL " +
                $"(Ozkaynak {totals.Equity:N2} + Net Kar {netIncome:N2}), " +
                $"Fark={difference:N2} TL");
        }

        return new BalanceSheetValidationResult(
            IsBalanced: isBalanced,
            TotalAssets: Math.Round(totals.Assets, 2),
            TotalLiabilities: Math.Round(totals.Liabilities, 2),
            TotalEquity: Math.Round(adjustedEquity, 2),
            Difference: Math.Round(difference, 2),
            Errors: errors.AsReadOnly());
    }

    private static BalanceSheetTotals CalculateTotals(
        IReadOnlyList<ChartOfAccounts> accounts,
        IReadOnlyList<JournalLine> postedLines,
        List<string> errors)
    {
        var totals = new BalanceSheetTotals();

        foreach (var account in accounts)
        {
            var accountLines = postedLines.Where(l => l.AccountId == account.Id).ToList();
            var debit = accountLines.Sum(l => l.Debit);
            var credit = accountLines.Sum(l => l.Credit);

            if (debit == 0 && credit == 0)
                continue;

            var balance = debit - credit;
            ClassifyAccountBalance(account, balance, totals, errors);
        }

        return totals;
    }

    private static void ClassifyAccountBalance(
        ChartOfAccounts account,
        decimal balance,
        BalanceSheetTotals totals,
        List<string> errors)
    {
        switch (account.AccountType)
        {
            case AccountType.Asset:
                totals.Assets += balance;
                if (balance < 0)
                {
                    errors.Add(
                        $"Varlik hesabi '{account.Code} - {account.Name}' negatif bakiye: {balance:N2} TL");
                }
                break;

            case AccountType.Liability:
                // Liability: credit-normal. balance = debit - credit, so -balance = net liability.
                totals.Liabilities += -balance;
                break;

            case AccountType.Equity:
                totals.Equity += -balance;
                break;

            case AccountType.Revenue:
                // Revenue: credit-normal. Net revenue = -balance.
                totals.Revenue += -balance;
                break;

            case AccountType.Expense:
                // Expense: debit-normal. Net expense = balance.
                totals.Expenses += balance;
                break;
        }
    }

    private sealed class BalanceSheetTotals
    {
        public decimal Assets { get; set; }
        public decimal Liabilities { get; set; }
        public decimal Equity { get; set; }
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
    }
}
