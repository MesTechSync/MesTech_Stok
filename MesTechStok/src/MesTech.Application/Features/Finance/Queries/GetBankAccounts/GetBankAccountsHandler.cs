using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Queries.GetBankAccounts;

public sealed class GetBankAccountsHandler
    : IRequestHandler<GetBankAccountsQuery, IReadOnlyList<BankAccountDto>>
{
    private readonly IBankAccountRepository _bankRepo;

    public GetBankAccountsHandler(IBankAccountRepository bankRepo)
        => _bankRepo = bankRepo ?? throw new ArgumentNullException(nameof(bankRepo));

    public async Task<IReadOnlyList<BankAccountDto>> Handle(
        GetBankAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _bankRepo.GetByTenantAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return accounts.Select(a => new BankAccountDto
        {
            Id = a.Id,
            BankName = a.BankName ?? string.Empty,
            AccountNumber = a.AccountNumber ?? string.Empty,
            IBAN = a.IBAN,
            CurrencyCode = a.Currency,
            Balance = a.Balance,
            IsActive = a.IsActive
        }).ToList();
    }
}
