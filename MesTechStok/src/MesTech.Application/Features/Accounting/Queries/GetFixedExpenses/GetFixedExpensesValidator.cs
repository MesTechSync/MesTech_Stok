using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetFixedExpenses;

public sealed class GetFixedExpensesValidator : AbstractValidator<GetFixedExpensesQuery>
{
    public GetFixedExpensesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
