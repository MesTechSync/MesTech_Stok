using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetChartOfAccounts;

public sealed class GetChartOfAccountsValidator : AbstractValidator<GetChartOfAccountsQuery>
{
    public GetChartOfAccountsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
