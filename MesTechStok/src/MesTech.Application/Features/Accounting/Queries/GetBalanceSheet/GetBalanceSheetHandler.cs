using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;

/// <summary>
/// Bilanco handler — hesap turune gore (Asset, Liability, Equity, Revenue, Expense)
/// bakiyeleri gruplar ve bolum bazinda satirlar olusturur.
/// Turkish THP mapping: 1xx=Varliklar, 2xx-3xx=Borclar, 5xx=Ozkaynaklar.
/// Muhasebe temel denklemi: Assets == Liabilities + Equity.
/// Revenue ve Expense net kar/zarar olarak Equity bolumune yansir.
/// </summary>
public sealed class GetBalanceSheetHandler : IRequestHandler<GetBalanceSheetQuery, BalanceSheetDto>
{
    private readonly IChartOfAccountsRepository _accountRepo;
    private readonly IJournalEntryRepository _journalRepo;

    public GetBalanceSheetHandler(
        IChartOfAccountsRepository accountRepo,
        IJournalEntryRepository journalRepo)
    {
        _accountRepo = accountRepo;
        _journalRepo = journalRepo;
    }

    public async Task<BalanceSheetDto> Handle(GetBalanceSheetQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var accounts = await _accountRepo.GetAllAsync(request.TenantId, isActive: true, cancellationToken);

        var entries = await _journalRepo.GetByDateRangeAsync(
            request.TenantId,
            DateTime.MinValue,
            request.AsOfDate,
            cancellationToken);

        var postedLines = entries
            .Where(e => e.IsPosted)
            .SelectMany(e => e.Lines)
            .ToList();

        var (assetLines, liabilityLines, equityLines) = ClassifyAccounts(accounts, postedLines);

        var sortedAssets = assetLines.OrderBy(l => l.AccountCode, StringComparer.Ordinal).ToList();
        var sortedLiabilities = liabilityLines.OrderBy(l => l.AccountCode, StringComparer.Ordinal).ToList();
        var sortedEquity = equityLines.OrderBy(l => l.AccountCode, StringComparer.Ordinal).ToList();

        var totalAssetsAmount = sortedAssets.Sum(l => l.Balance);
        var totalLiabilitiesAmount = sortedLiabilities.Sum(l => l.Balance);
        var totalEquityAmount = sortedEquity.Sum(l => l.Balance);

        return new BalanceSheetDto
        {
            AsOfDate = request.AsOfDate,
            Assets = new BalanceSheetSectionDto
            {
                SectionName = "Varliklar",
                Lines = sortedAssets.AsReadOnly(),
                Total = totalAssetsAmount
            },
            Liabilities = new BalanceSheetSectionDto
            {
                SectionName = "Borclar",
                Lines = sortedLiabilities.AsReadOnly(),
                Total = totalLiabilitiesAmount
            },
            Equity = new BalanceSheetSectionDto
            {
                SectionName = "Ozkaynaklar",
                Lines = sortedEquity.AsReadOnly(),
                Total = totalEquityAmount
            },
            IsBalanced = totalAssetsAmount == totalLiabilitiesAmount + totalEquityAmount
        };
    }

    private static (List<BalanceSheetLineDto> Assets, List<BalanceSheetLineDto> Liabilities, List<BalanceSheetLineDto> Equity)
        ClassifyAccounts(
            IReadOnlyList<ChartOfAccounts> accounts,
            List<JournalLine> postedLines)
    {
        var assetLines = new List<BalanceSheetLineDto>();
        var liabilityLines = new List<BalanceSheetLineDto>();
        var equityLines = new List<BalanceSheetLineDto>();

        decimal totalRevenue = 0;
        decimal totalExpenses = 0;

        foreach (var account in accounts)
        {
            var accountLines = postedLines.Where(l => l.AccountId == account.Id).ToList();
            var debit = accountLines.Sum(l => l.Debit);
            var credit = accountLines.Sum(l => l.Credit);

            if (debit == 0 && credit == 0)
                continue;

            var balance = debit - credit;
            ProcessAccount(account, balance, assetLines, liabilityLines, equityLines, ref totalRevenue, ref totalExpenses);
        }

        var netIncome = totalRevenue - totalExpenses;
        if (netIncome != 0)
        {
            var netIncomeAccount = accounts.FirstOrDefault(a =>
                a.Code.StartsWith("590", StringComparison.Ordinal));

            equityLines.Add(new BalanceSheetLineDto
            {
                AccountId = netIncomeAccount?.Id ?? Guid.NewGuid(),
                AccountCode = netIncomeAccount?.Code ?? "590",
                AccountName = netIncomeAccount?.Name ?? "Donem Net Kari (Zarari)",
                Balance = netIncome
            });
        }

        return (assetLines, liabilityLines, equityLines);
    }

    private static void ProcessAccount(
        ChartOfAccounts account,
        decimal balance,
        List<BalanceSheetLineDto> assetLines,
        List<BalanceSheetLineDto> liabilityLines,
        List<BalanceSheetLineDto> equityLines,
        ref decimal totalRevenue,
        ref decimal totalExpenses)
    {
        switch (account.AccountType)
        {
            case AccountType.Asset:
                assetLines.Add(new BalanceSheetLineDto
                {
                    AccountId = account.Id,
                    AccountCode = account.Code,
                    AccountName = account.Name,
                    Balance = balance
                });
                break;

            case AccountType.Liability:
                liabilityLines.Add(new BalanceSheetLineDto
                {
                    AccountId = account.Id,
                    AccountCode = account.Code,
                    AccountName = account.Name,
                    Balance = -balance
                });
                break;

            case AccountType.Equity:
                equityLines.Add(new BalanceSheetLineDto
                {
                    AccountId = account.Id,
                    AccountCode = account.Code,
                    AccountName = account.Name,
                    Balance = -balance
                });
                break;

            case AccountType.Revenue:
                totalRevenue += -balance;
                break;

            case AccountType.Expense:
                totalExpenses += balance;
                break;
        }
    }
}
