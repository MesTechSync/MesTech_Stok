using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetAccountBalance;

public sealed class GetAccountBalanceValidator : AbstractValidator<GetAccountBalanceQuery>
{
    public GetAccountBalanceValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.AccountId).NotEqual(Guid.Empty);
    }
}
