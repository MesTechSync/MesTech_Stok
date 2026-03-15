using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetTrialBalance;

/// <summary>
/// Mizan handler — her hesap icin JournalLine borc/alacak toplamini hesaplar.
/// Acilis bakiyesi: donem oncesi kayitlar.
/// Donem hareketi: StartDate-EndDate arasindaki kayitlar.
/// Kapanis bakiyesi: Acilis + Donem toplami.
/// Sadece IsPosted == true olan kayitlar dahil edilir.
/// </summary>
public class GetTrialBalanceHandler : IRequestHandler<GetTrialBalanceQuery, TrialBalanceDto>
{
    private readonly IChartOfAccountsRepository _accountRepo;
    private readonly IJournalEntryRepository _journalRepo;

    public GetTrialBalanceHandler(
        IChartOfAccountsRepository accountRepo,
        IJournalEntryRepository journalRepo)
    {
        _accountRepo = accountRepo;
        _journalRepo = journalRepo;
    }

    public async Task<TrialBalanceDto> Handle(GetTrialBalanceQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _accountRepo.GetAllAsync(request.TenantId, isActive: true, cancellationToken);

        // Opening balance: all posted entries before the period start
        var openingEntries = await _journalRepo.GetByDateRangeAsync(
            request.TenantId,
            DateTime.MinValue,
            request.StartDate.AddDays(-1),
            cancellationToken);

        var openingLines = openingEntries
            .Where(e => e.IsPosted)
            .SelectMany(e => e.Lines)
            .ToList();

        // Period entries: posted entries within the period
        var periodEntries = await _journalRepo.GetByDateRangeAsync(
            request.TenantId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        var periodLines = periodEntries
            .Where(e => e.IsPosted)
            .SelectMany(e => e.Lines)
            .ToList();

        var lines = new List<TrialBalanceLineDto>();

        foreach (var account in accounts)
        {
            var accOpeningLines = openingLines.Where(l => l.AccountId == account.Id).ToList();
            var accPeriodLines = periodLines.Where(l => l.AccountId == account.Id).ToList();

            var openingDebit = accOpeningLines.Sum(l => l.Debit);
            var openingCredit = accOpeningLines.Sum(l => l.Credit);
            var periodDebit = accPeriodLines.Sum(l => l.Debit);
            var periodCredit = accPeriodLines.Sum(l => l.Credit);

            // Only include accounts with any activity
            if (openingDebit == 0 && openingCredit == 0 && periodDebit == 0 && periodCredit == 0)
                continue;

            lines.Add(new TrialBalanceLineDto
            {
                AccountId = account.Id,
                AccountCode = account.Code,
                AccountName = account.Name,
                AccountType = account.AccountType.ToString(),
                OpeningDebit = openingDebit,
                OpeningCredit = openingCredit,
                PeriodDebit = periodDebit,
                PeriodCredit = periodCredit,
                ClosingDebit = openingDebit + periodDebit,
                ClosingCredit = openingCredit + periodCredit
            });
        }

        // Sort by account code for standard mizan format
        var sortedLines = lines.OrderBy(l => l.AccountCode).ToList();

        return new TrialBalanceDto
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Lines = sortedLines.AsReadOnly(),
            GrandTotalOpeningDebit = sortedLines.Sum(l => l.OpeningDebit),
            GrandTotalOpeningCredit = sortedLines.Sum(l => l.OpeningCredit),
            GrandTotalPeriodDebit = sortedLines.Sum(l => l.PeriodDebit),
            GrandTotalPeriodCredit = sortedLines.Sum(l => l.PeriodCredit),
            GrandTotalClosingDebit = sortedLines.Sum(l => l.ClosingDebit),
            GrandTotalClosingCredit = sortedLines.Sum(l => l.ClosingCredit)
        };
    }
}
