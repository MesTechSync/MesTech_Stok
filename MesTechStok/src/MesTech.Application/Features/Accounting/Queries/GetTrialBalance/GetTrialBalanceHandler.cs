using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;

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
        ArgumentNullException.ThrowIfNull(request);
        var accounts = await _accountRepo.GetAllAsync(request.TenantId, isActive: true, cancellationToken);

        var openingLines = await GetPostedLinesAsync(request.TenantId, DateTime.MinValue, request.StartDate.AddDays(-1), cancellationToken);
        var periodLines = await GetPostedLinesAsync(request.TenantId, request.StartDate, request.EndDate, cancellationToken);

        var lines = BuildTrialBalanceLines(accounts, openingLines, periodLines);
        var sortedLines = lines.OrderBy(l => l.AccountCode, StringComparer.Ordinal).ToList();

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

    private async Task<List<JournalLine>> GetPostedLinesAsync(
        Guid tenantId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var entries = await _journalRepo.GetByDateRangeAsync(tenantId, from, to, cancellationToken);
        return entries
            .Where(e => e.IsPosted)
            .SelectMany(e => e.Lines)
            .ToList();
    }

    private static List<TrialBalanceLineDto> BuildTrialBalanceLines(
        IReadOnlyList<ChartOfAccounts> accounts,
        List<JournalLine> openingLines,
        List<JournalLine> periodLines)
    {
        var lines = new List<TrialBalanceLineDto>();

        foreach (var account in accounts)
        {
            var lineDto = BuildAccountLine(account, openingLines, periodLines);
            if (lineDto is not null)
                lines.Add(lineDto);
        }

        return lines;
    }

    private static TrialBalanceLineDto? BuildAccountLine(
        ChartOfAccounts account,
        List<JournalLine> openingLines,
        List<JournalLine> periodLines)
    {
        var accOpeningLines = openingLines.Where(l => l.AccountId == account.Id).ToList();
        var accPeriodLines = periodLines.Where(l => l.AccountId == account.Id).ToList();

        var openingDebit = accOpeningLines.Sum(l => l.Debit);
        var openingCredit = accOpeningLines.Sum(l => l.Credit);
        var periodDebit = accPeriodLines.Sum(l => l.Debit);
        var periodCredit = accPeriodLines.Sum(l => l.Credit);

        if (openingDebit == 0 && openingCredit == 0 && periodDebit == 0 && periodCredit == 0)
            return null;

        return new TrialBalanceLineDto
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
        };
    }
}
