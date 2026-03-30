using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetTrialBalance;

public sealed class GetTrialBalanceValidator : AbstractValidator<GetTrialBalanceQuery>
{
    public GetTrialBalanceValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StartDate).LessThanOrEqualTo(x => x.EndDate);
    }
}
