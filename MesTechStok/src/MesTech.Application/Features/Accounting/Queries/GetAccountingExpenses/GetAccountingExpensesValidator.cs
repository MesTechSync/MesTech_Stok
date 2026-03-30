using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetAccountingExpenses;

public sealed class GetAccountingExpensesValidator : AbstractValidator<GetAccountingExpensesQuery>
{
    public GetAccountingExpensesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.From).NotEmpty();
        RuleFor(x => x.To).NotEmpty().GreaterThanOrEqualTo(x => x.From);
    }
}
