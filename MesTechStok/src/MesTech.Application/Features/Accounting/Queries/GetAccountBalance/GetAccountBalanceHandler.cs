using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using IJournalEntryRepository = MesTech.Domain.Interfaces.IJournalEntryRepository;

namespace MesTech.Application.Features.Accounting.Queries.GetAccountBalance;

public sealed class GetAccountBalanceHandler : IRequestHandler<GetAccountBalanceQuery, AccountBalanceDto?>
{
    private readonly IChartOfAccountsRepository _accountRepo;
    private readonly IJournalEntryRepository _journalRepo;

    public GetAccountBalanceHandler(IChartOfAccountsRepository accountRepo, IJournalEntryRepository journalRepo)
    {
        _accountRepo = accountRepo;
        _journalRepo = journalRepo;
    }

    public async Task<AccountBalanceDto?> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var account = await _accountRepo.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null) return null;

        var entries = await _journalRepo.GetByAccountIdAsync(request.TenantId, request.AccountId, cancellationToken);

        var totalDebit = entries.SelectMany(e => e.Lines)
            .Where(l => l.AccountId == request.AccountId)
            .Sum(l => l.Debit);

        var totalCredit = entries.SelectMany(e => e.Lines)
            .Where(l => l.AccountId == request.AccountId)
            .Sum(l => l.Credit);

        return new AccountBalanceDto
        {
            AccountId = account.Id,
            Code = account.Code,
            Name = account.Name,
            AccountType = account.AccountType.ToString(),
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            Balance = totalDebit - totalCredit
        };
    }
}
