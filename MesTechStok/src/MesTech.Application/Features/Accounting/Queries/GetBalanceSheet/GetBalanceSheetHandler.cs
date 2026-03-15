using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;

/// <summary>
/// Bilanco handler — hesap turune gore (Asset, Liability, Equity, Revenue, Expense)
/// bakiyeleri gruplar ve bolum bazinda satirlar olusturur.
/// Turkish THP mapping: 1xx=Varliklar, 2xx-3xx=Borclar, 5xx=Ozkaynaklar.
/// Muhasebe temel denklemi: Assets == Liabilities + Equity.
/// Revenue ve Expense net kar/zarar olarak Equity bolumune yansir.
/// </summary>
public class GetBalanceSheetHandler : IRequestHandler<GetBalanceSheetQuery, BalanceSheetDto>
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
        var accounts = await _accountRepo.GetAllAsync(request.TenantId, isActive: true, cancellationToken);

        // Get all posted journal entries up to the requested date
        var entries = await _journalRepo.GetByDateRangeAsync(
            request.TenantId,
            DateTime.MinValue,
            request.AsOfDate,
            cancellationToken);

        var postedLines = entries
            .Where(e => e.IsPosted)
            .SelectMany(e => e.Lines)
            .ToList();

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
            var balance = debit - credit;

            // Skip accounts with no activity
            if (debit == 0 && credit == 0)
                continue;

            switch (account.AccountType)
            {
                case AccountType.Asset:
                    // Asset accounts: normal balance is debit (positive)
                    assetLines.Add(new BalanceSheetLineDto
                    {
                        AccountId = account.Id,
                        AccountCode = account.Code,
                        AccountName = account.Name,
                        Balance = balance
                    });
                    break;

                case AccountType.Liability:
                    // Liability accounts: normal balance is credit (negative of debit-credit)
                    liabilityLines.Add(new BalanceSheetLineDto
                    {
                        AccountId = account.Id,
                        AccountCode = account.Code,
                        AccountName = account.Name,
                        Balance = -balance
                    });
                    break;

                case AccountType.Equity:
                    // Equity accounts: normal balance is credit
                    equityLines.Add(new BalanceSheetLineDto
                    {
                        AccountId = account.Id,
                        AccountCode = account.Code,
                        AccountName = account.Name,
                        Balance = -balance
                    });
                    break;

                case AccountType.Revenue:
                    // Revenue: normal balance is credit — accumulates into net income
                    totalRevenue += -balance;
                    break;

                case AccountType.Expense:
                    // Expense: normal balance is debit — accumulates into net income
                    totalExpenses += balance;
                    break;
            }
        }

        // Net income flows into Equity section as retained earnings
        var netIncome = totalRevenue - totalExpenses;
        if (netIncome != 0)
        {
            equityLines.Add(new BalanceSheetLineDto
            {
                AccountId = Guid.Empty,
                AccountCode = "590",
                AccountName = "Donem Net Kari (Zarari)",
                Balance = netIncome
            });
        }

        var sortedAssets = assetLines.OrderBy(l => l.AccountCode).ToList();
        var sortedLiabilities = liabilityLines.OrderBy(l => l.AccountCode).ToList();
        var sortedEquity = equityLines.OrderBy(l => l.AccountCode).ToList();

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
}
