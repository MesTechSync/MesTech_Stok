using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;

public sealed class GetIncomeExpenseListValidator : AbstractValidator<GetIncomeExpenseListQuery>
{
    public GetIncomeExpenseListValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
