using FluentValidation;

namespace MesTech.Application.Features.Finance.Queries.GetBudgetSummary;

public sealed class GetBudgetSummaryValidator : AbstractValidator<GetBudgetSummaryQuery>
{
    public GetBudgetSummaryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2020, 2099);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
