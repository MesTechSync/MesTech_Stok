using FluentValidation;

namespace MesTech.Application.Features.Finance.Queries.GetBankAccounts;

public sealed class GetBankAccountsValidator : AbstractValidator<GetBankAccountsQuery>
{
    public GetBankAccountsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
