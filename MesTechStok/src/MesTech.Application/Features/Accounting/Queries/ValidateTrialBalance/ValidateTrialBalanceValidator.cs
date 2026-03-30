using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.ValidateTrialBalance;

public sealed class ValidateTrialBalanceValidator : AbstractValidator<ValidateTrialBalanceQuery>
{
    public ValidateTrialBalanceValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.StartDate).LessThanOrEqualTo(x => x.EndDate);
    }
}
