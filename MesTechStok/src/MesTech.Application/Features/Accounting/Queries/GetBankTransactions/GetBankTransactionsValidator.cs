using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetBankTransactions;

public sealed class GetBankTransactionsValidator : AbstractValidator<GetBankTransactionsQuery>
{
    public GetBankTransactionsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.BankAccountId).NotEqual(Guid.Empty);
    }
}
